using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Utilities;

namespace JetBlack.Bloomberg.Managers
{
    internal class SubscriptionManager : ISubscriptionProvider
    {
        private readonly Session _session;
        private readonly Identity _identity;

        private readonly IDictionary<CorrelationID, IObserver<TickerData>> _subscriptions = new Dictionary<CorrelationID, IObserver<TickerData>>();

        public SubscriptionManager(Session session, Identity identity)
        {
            _session = session;
            _identity = identity;
        }

        public IObservable<TickerData> ToObservable(IEnumerable<string> tickers, IEnumerable<string> fields)
        {
            return Observable.Create<TickerData>(observer =>
            {
                var uniqueFields = fields.Distinct().ToArray();
                var subscriptions = new List<Subscription>();
                foreach (var ticker in tickers.Distinct())
                {
                    var correlationId = new CorrelationID();
                    _subscriptions.Add(correlationId, observer);

                    subscriptions.Add(new Subscription(ticker, uniqueFields, correlationId));
                }
                _session.Subscribe(subscriptions, _identity);

                return Disposable.Create(() =>
                {
                    subscriptions.ForEach(x => _subscriptions.Remove(x.CorrelationID));
                    _session.Unsubscribe(subscriptions);
                });
            });
        }

        public void ProcessSubscriptionData(Session session, Message message, bool isPartialResponse)
        {
            IObserver<TickerData> observer;
            if (!_subscriptions.TryGetValue(message.CorrelationID, out observer))
                return;

            observer.OnNext(new TickerData(message.TopicName, message.Elements.ToDictionary(x => x.Name.ToString(), y => y.GetFieldValue())));
        }

        public void ProcessSubscriptionStatus(Session session, Message message)
        {
            IObserver<TickerData> observer;
            if (!_subscriptions.TryGetValue(message.CorrelationID, out observer))
                return;

            if (MessageTypeNames.SubscriptionFailure.Equals(message.MessageType))
            {
                _subscriptions.Remove(message.CorrelationID);
                observer.OnError(new EventArgsException<SubscriptionFailureEventArgs>(message.ToSubscriptionFailureEventArgs()));
            }
            else if (MessageTypeNames.SubscriptionTerminated.Equals(message.MessageType))
            {
                var reason = message.GetElement(ElementNames.Reason);
                switch (reason.GetElementAsString(ElementNames.Category))
                {
                    case "LIMIT":
                        _subscriptions.Remove(message.CorrelationID);
                        observer.OnError(new EventArgsException<SubscriptionFailureEventArgs>("Concurrent subscription limit has been exceeded.", message.ToSubscriptionFailureEventArgs()));
                        break;
                    case "UNCLASSIFIED":
                        /*
                         * "Failed to obtain initial paint"
                         * If this error occurs, the Bloomberg Data Center
                         * was unable to get the initial paint for the
                         * subscription. You will still receive subscription
                         * ticks.
                         */
                        break;
                    case "CANCELED":
                        _subscriptions.Remove(message.CorrelationID);
                        observer.OnCompleted();
                        break;
                }
            }
            else if (MessageTypeNames.SubscriptionStarted.Equals(message.MessageType))
            {
                //if (message.HasElement(ElementNames.Exceptions))
                //{
                //    // Field subscription failures
                //    var fieldExceptions = new List<KeyValuePair<string, Failure>>();

                //    var exceptions = message.GetElement(ElementNames.Exceptions);
                //    for (var i = 0; i < exceptions.NumValues; ++i)
                //    {
                //        var exception = exceptions.GetValueAsElement(i);
                //        fieldExceptions.Add(KeyValuePair.Create(exception.GetElement(ElementNames.FieldId).GetValueAsString(), new Failure(exception.GetElement(ElementNames.Reason))));
                //    }

                //    observer.OnNext(new SessionTopicStatusMessage(session, message.TopicName, fieldExceptions));
                //}
            }
        }
    }
}
