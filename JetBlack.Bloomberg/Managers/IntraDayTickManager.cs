using System;
using System.Collections.Generic;
using System.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Bloomberg.Requesters;
using JetBlack.Bloomberg.Utilities;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    public class IntradayTickManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<TickerIntradayTickData>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<TickerIntradayTickData>>();
        private readonly IDictionary<CorrelationID, string> _tickerMap = new Dictionary<CorrelationID, string>(); 
        private readonly IDictionary<CorrelationID, TickerIntradayTickData> _partial = new Dictionary<CorrelationID, TickerIntradayTickData>();

        public IPromise<TickerIntradayTickData> Request(Session session, Identity identity, Service refDataService, IntradayTickRequestFactory requestFactory)
        {
            return new Promise<TickerIntradayTickData>((resolve, reject) =>
            {
                var request = requestFactory.CreateRequest(refDataService);
                var correlationId = new CorrelationID();
                _asyncHandlers.Add(correlationId, AsyncPattern<TickerIntradayTickData>.Create(resolve, reject));
                _tickerMap.Add(correlationId, requestFactory.Ticker);
                session.SendRequest(request, identity, correlationId);
            });
        }

        public void Process(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<TickerIntradayTickData> asyncHandler;
            if (!_asyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
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

            var tickData = message.GetElement("tickData").GetElement("tickData");

            TickerIntradayTickData tickerIntradayTickData;
            if (_partial.TryGetValue(message.CorrelationID, out tickerIntradayTickData))
                _partial.Remove(message.CorrelationID);
            else
            {
                var entitlementIds = tickData.HasElement("eidData") ? tickData.GetElement("eidData").ExtractEids() : null;
                tickerIntradayTickData = new TickerIntradayTickData(ticker, new List<IntradayTickData>(), entitlementIds);
            }

            for (var i = 0; i < tickData.NumValues; ++i)
            {
                var item = tickData.GetValueAsElement(i);
                var conditionCodes = (item.HasElement("conditionCodes") ? item.GetElementAsString("conditionCodes").Split(',') : null);
                var exchangeCodes = (item.HasElement("exchangeCode") ? item.GetElementAsString("exchangeCode").Split(',') : null);

                tickerIntradayTickData.IntraDayTicks.Add(
                    new IntradayTickData(
                        item.GetElementAsDatetime("time").ToDateTime(),
                        (EventType)Enum.Parse(typeof(EventType), item.GetElementAsString("type"), true),
                        item.GetElementAsFloat64("value"),
                        item.GetElementAsInt32("size"),
                        conditionCodes,
                        exchangeCodes));
            }

            if (isPartialResponse)
            {
                _tickerMap.Add(message.CorrelationID, ticker);
                _partial[message.CorrelationID] = tickerIntradayTickData;
            }
            else
                asyncHandler.OnSuccess(tickerIntradayTickData);
        }
    }
}
