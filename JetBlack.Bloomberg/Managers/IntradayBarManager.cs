using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Utilities;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    internal class IntradayBarManager : RequestResponseManager<IntradayBarRequest, IntradayBarResponse>, IIntradayBarProvider
    {
        private readonly Service _service;
        private readonly Identity _identity;

        private readonly IDictionary<CorrelationID, string> _tickerMap = new Dictionary<CorrelationID, string>();
        private readonly IDictionary<CorrelationID, IntradayBarResponse> _partial = new Dictionary<CorrelationID, IntradayBarResponse>();

        public IntradayBarManager(Session session, Service service, Identity identity)
            : base(session)
        {
            _service = service;
            _identity = identity;
        }

        public override IPromise<IntradayBarResponse> Request(IntradayBarRequest request)
        {
            return new Promise<IntradayBarResponse>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                AsyncHandlers.Add(correlationId, AsyncPattern<IntradayBarResponse>.Create(resolve, reject));
                _tickerMap.Add(correlationId, request.Ticker);
                Session.SendRequest(request.ToRequest(_service), _identity, correlationId);
            });
        }

        public override void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<IntradayBarResponse> asyncHandler;
            if (!AsyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            var ticker = _tickerMap[message.CorrelationID];
            _tickerMap.Remove(message.CorrelationID);

            if (message.HasElement(ElementNames.ResponseError))
            {
                asyncHandler.OnFailure(new ContentException<TickerResponseError>(new TickerResponseError(ticker, message.GetElement(ElementNames.ResponseError).ToResponseError())));
                return;
            }

            var barData = message.GetElement(ElementNames.BarData);

            IntradayBarResponse intradayBarResponse;
            if (_partial.TryGetValue(message.CorrelationID, out intradayBarResponse))
                _partial.Remove(message.CorrelationID);
            else
            {
                var entitlementIds = barData.HasElement(ElementNames.EidData) ? barData.GetElement(ElementNames.EidData).ExtractEids() : null;
                intradayBarResponse = new IntradayBarResponse(ticker, new List<IntradayBar>(), entitlementIds);
            }

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

            if (isPartialResponse)
            {
                _tickerMap.Add(message.CorrelationID, ticker);
                _partial[message.CorrelationID] = intradayBarResponse;
            }
            else
                asyncHandler.OnSuccess(intradayBarResponse);
        }
    }
}
