using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Responses;
using JetBlack.Bloomberg.Utilities;

namespace JetBlack.Bloomberg.Managers
{
    internal class SubscriptionManager : ObserverManager<SubscriptionResponse>, ISubscriptionProvider
    {
        public SubscriptionManager(Session session, Identity identity)
            : base(session, identity)
        {
        }

        public IObservable<SubscriptionResponse> ToObservable(IEnumerable<SubscriptionRequest> subscriptionRequests)
        {
            return Observable.Create<SubscriptionResponse>(observer =>
            {
                var subscriptions = new List<Subscription>();
                foreach (var subscriptionRequest in subscriptionRequests)
                {
                    var correlationId = new CorrelationID();
                    Add(correlationId, observer);

                    subscriptions.Add(new Subscription(subscriptionRequest.Security, subscriptionRequest.Fields, correlationId));
                }
                Session.Subscribe(subscriptions, Identity);

                return Disposable.Create(() =>
                {
                    subscriptions.ForEach(x => Remove(x.CorrelationID));
                    Session.Unsubscribe(subscriptions);
                });
            });
        }

        public void ProcessSubscriptionData(Session session, Message message)
        {
            IObserver<SubscriptionResponse> observer;
            if (!TryGet(message.CorrelationID, out observer))
                return;

            IDictionary<string, object> data = message.Elements.ToDictionary(x => x.Name.ToString(), y => y.GetFieldValue());
            observer.OnNext(new SubscriptionResponse(message.TopicName, data));
        }

        public void ProcessSubscriptionStatus(Session session, Message message)
        {
            IObserver<SubscriptionResponse> observer;
            if (!TryGet(message.CorrelationID, out observer))
                return;

            if (MessageTypeNames.SubscriptionFailure.Equals(message.MessageType))
            {
                var reasonElement = message.GetElement(ElementNames.Reason);
                var error = new ResponseError(
                    reasonElement.GetElement(ElementNames.Source).GetValueAsString(),
                    reasonElement.GetElement(ElementNames.Category).GetValueAsString(),
                    null,
                    reasonElement.GetElement(ElementNames.ErrorCode).GetValueAsInt32(),
                    reasonElement.GetElement(ElementNames.Description).GetValueAsString());
                var subscriptionFailure = new SubscriptionFailure(error);
                observer.OnNext(new SubscriptionResponse(message.TopicName, subscriptionFailure));
            }
            else if (MessageTypeNames.SubscriptionTerminated.Equals(message.MessageType))
            {
                var reason = message.GetElement(ElementNames.Reason);
                switch (reason.GetElementAsString(ElementNames.Category))
                {
                    case "LIMIT":
                        var responseError = new ResponseError(
                            reason.GetElement(ElementNames.Source).GetValueAsString(),
                            reason.GetElement(ElementNames.Category).GetValueAsString(),
                            null,
                            reason.GetElement(ElementNames.ErrorCode).GetValueAsInt32(),
                            reason.GetElement(ElementNames.Description).GetValueAsString());
                        observer.OnNext(new SubscriptionResponse(message.TopicName, new SubscriptionFailure(responseError)));
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
                        Remove(message.CorrelationID);
                        observer.OnCompleted();
                        break;
                }
            }
            else if (MessageTypeNames.SubscriptionStarted.Equals(message.MessageType))
            {
                if (message.HasElement(ElementNames.Exceptions))
                {
                    var fieldErrors = new Dictionary<string, ResponseError>();
                    var exceptionsArrayElement = message.GetElement(ElementNames.Exceptions);
                    for (var i = 0; i < exceptionsArrayElement.NumValues; ++i)
                    {
                        var exceptionsElement = exceptionsArrayElement.GetValueAsElement(i);
                        var fieldId = exceptionsElement.GetElementAsString(ElementNames.FieldId);
                        var reasonElement = exceptionsElement.GetElement(ElementNames.Reason);
                        var error = new ResponseError(
                            reasonElement.GetElement(ElementNames.Source).GetValueAsString(),
                            reasonElement.GetElement(ElementNames.Category).GetValueAsString(),
                            reasonElement.GetElement(ElementNames.SubCategory).GetValueAsString(),
                            reasonElement.GetElement(ElementNames.ErrorCode).GetValueAsInt32(),
                            reasonElement.GetElement(ElementNames.Description).GetValueAsString());
                        fieldErrors.Add(fieldId, error);
                    }
                    var subscriptionFailure = new SubscriptionFailure(fieldErrors);
                    observer.OnNext(new SubscriptionResponse(message.TopicName, subscriptionFailure));
                }
            }
        }
    }
}
