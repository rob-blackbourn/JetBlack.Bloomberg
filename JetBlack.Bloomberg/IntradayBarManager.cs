using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Bloomberg.Requesters;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public class IntradayBarManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<TickerIntradayBarData>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<TickerIntradayBarData>>();
        private readonly IDictionary<CorrelationID, TickerIntradayBarData> _partial = new Dictionary<CorrelationID, TickerIntradayBarData>();

        public IPromise<TickerIntradayBarData> Request(Session session, Service refDataService, IntradayBarRequestFactory requestFactory)
        {
            return new Promise<TickerIntradayBarData>((resolve, reject) =>
            {
                var requests = requestFactory.CreateRequests(refDataService);

                foreach (var request in requests)
                {
                    var correlationId = new CorrelationID();
                    _asyncHandlers.Add(correlationId, AsyncPattern<TickerIntradayBarData>.Create(resolve, reject));
                    session.SendRequest(request, correlationId);
                }
            });
        }

        public void Process(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<TickerIntradayBarData> asyncHandler;
            if (!_asyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            var ticker = message.TopicName;

            if (!message.HasElement(ElementNames.ResponseError))
            {
                asyncHandler.OnFailure(new ContentException<TickerResponseError>(new TickerResponseError(ticker, message.GetElement(ElementNames.ResponseError).ToResponseError())));
                return;
            }

            var barData = message.GetElement("barData");

            TickerIntradayBarData tickerIntradayBarData;
            if (_partial.TryGetValue(message.CorrelationID, out tickerIntradayBarData))
                _partial.Remove(message.CorrelationID);
            else
            {
                var entitlementIds = barData.HasElement("eidData") ? barData.GetElement("eidData").ExtractEids() : null;
                tickerIntradayBarData = new TickerIntradayBarData(ticker, new List<IntradayBar>(), entitlementIds);
            }

            var barTickData = barData.GetElement("barTickData");

            for (var i = 0; i < barTickData.NumValues; ++i)
            {
                var element = barTickData.GetValueAsElement(i);
                tickerIntradayBarData.IntradayBars.Add(
                    new IntradayBar(
                        element.GetElementAsDatetime("time").ToDateTime(),
                        element.GetElementAsFloat64("open"),
                        element.GetElementAsFloat64("high"),
                        element.GetElementAsFloat64("low"),
                        element.GetElementAsFloat64("close"),
                        element.GetElementAsInt32("numEvents"),
                        element.GetElementAsInt64("volume")));
            }

            if (isPartialResponse)
                _partial[message.CorrelationID] = tickerIntradayBarData;
            else
                asyncHandler.OnSuccess(tickerIntradayBarData);
        }
    }
}
