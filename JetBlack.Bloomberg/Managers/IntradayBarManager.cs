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
            _tickerMap.Remove(message.CorrelationID);

            if (message.HasElement(ElementNames.ResponseError))
            {
                observer.OnError(new ContentException<TickerResponseError>(new TickerResponseError(ticker, message.GetElement(ElementNames.ResponseError).ToResponseError())));
                return;
            }

            var barData = message.GetElement(ElementNames.BarData);

            var entitlementIds = barData.HasElement(ElementNames.EidData) ? barData.GetElement(ElementNames.EidData).ExtractEids() : null;
            var intradayBarResponse = new IntradayBarResponse(ticker, new List<IntradayBar>(), entitlementIds);

            var barTickData = barData.GetElement(ElementNames.BarTickData);

            for (var i = 0; i < barTickData.NumValues; ++i)
            {
                var element = barTickData.GetValueAsElement(i);
                intradayBarResponse.IntradayBars.Add(
                    new IntradayBar(
                        element.GetElementAsDatetime(ElementNames.Time).ToDateTime(),
                        element.GetElementAsFloat64(ElementNames.Open),
                        element.GetElementAsFloat64(ElementNames.High),
                        element.GetElementAsFloat64(ElementNames.Low),
                        element.GetElementAsFloat64(ElementNames.Close),
                        element.GetElementAsInt32(ElementNames.NumEvents),
                        element.GetElementAsInt64(ElementNames.Volume)));
            }

            observer.OnNext(intradayBarResponse);

            if (!isPartialResponse)
            {
                observer.OnCompleted();
                Observers.Remove(message.CorrelationID);
            }
        }
    }
}
