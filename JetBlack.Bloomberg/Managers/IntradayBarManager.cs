using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Utilities;

namespace JetBlack.Bloomberg.Managers
{
    internal class IntradayBarManager : RequestResponseManager<IntradayBarRequest, IntradayBarResponse>, IIntradayBarProvider
    {
        private readonly Service _service;
        private readonly Identity _identity;

        private readonly IDictionary<CorrelationID, string> _tickerMap = new Dictionary<CorrelationID, string>();

        public IntradayBarManager(Session session, Service service, Identity identity)
            : base(session)
        {
            _service = service;
            _identity = identity;
        }

        public override IObservable<IntradayBarResponse> ToObservable(IntradayBarRequest request)
        {
            return Observable.Create<IntradayBarResponse>(observer =>
            {
                var correlationId = new CorrelationID();
                Observers.Add(correlationId, observer);
                _tickerMap.Add(correlationId, request.Ticker);
                Session.SendRequest(request.ToRequest(_service), _identity, correlationId);
                return Disposable.Create(() => Session.Cancel(correlationId));
            });
        }

        public override bool CanProcessResponse(Message message)
        {
            return MessageTypeNames.IntradayBarResponse.Equals(message.MessageType);
        }

        public override void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            IObserver<IntradayBarResponse> observer;
            if (!Observers.TryGetValue(message.CorrelationID, out observer))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            var ticker = _tickerMap[message.CorrelationID];

            if (message.HasElement(ElementNames.ResponseError))
            {
                var responseErrorElement = message.GetElement(ElementNames.ResponseError);
                var responseError = new ResponseError(
                    responseErrorElement.GetElementAsString(ElementNames.Source),
                    responseErrorElement.GetElementAsString(ElementNames.Category),
                    responseErrorElement.GetElementAsString(ElementNames.SubCategory),
                    responseErrorElement.GetElementAsInt32(ElementNames.Code),
                    responseErrorElement.GetElementAsString(ElementNames.Message));
                observer.OnError(new ContentException<TickerResponseError>(new TickerResponseError(ticker, responseError)));

                // We assume nore more messages will be sent for this correlation id.
                Observers.Remove(message.CorrelationID);
                _tickerMap.Remove(message.CorrelationID);

                return;
            }

            var barDataElement = message.GetElement(ElementNames.BarData);

            var entitlementIds = barDataElement.HasElement(ElementNames.EidData) ? barDataElement.GetElement(ElementNames.EidData).ExtractEids() : null;

            var barTickDataArrayElement = barDataElement.GetElement(ElementNames.BarTickData);

            var intradayBars = new List<IntradayBar>();
            for (var i = 0; i < barTickDataArrayElement.NumValues; ++i)
            {
                var barTickDataElement = barTickDataArrayElement.GetValueAsElement(i);
                intradayBars.Add(
                    new IntradayBar(
                        barTickDataElement.GetElementAsDatetime(ElementNames.Time).ToDateTime(),
                        barTickDataElement.GetElementAsFloat64(ElementNames.Open),
                        barTickDataElement.GetElementAsFloat64(ElementNames.High),
                        barTickDataElement.GetElementAsFloat64(ElementNames.Low),
                        barTickDataElement.GetElementAsFloat64(ElementNames.Close),
                        barTickDataElement.GetElementAsInt32(ElementNames.NumEvents),
                        barTickDataElement.GetElementAsInt64(ElementNames.Volume),
                        barTickDataElement.GetElementAsFloat64(ElementNames.Value)));
            }

            observer.OnNext(new IntradayBarResponse(ticker, intradayBars, entitlementIds));

            if (!isPartialResponse)
            {
                observer.OnCompleted();
                Observers.Remove(message.CorrelationID);
                _tickerMap.Remove(message.CorrelationID);
            }
        }
    }
}
