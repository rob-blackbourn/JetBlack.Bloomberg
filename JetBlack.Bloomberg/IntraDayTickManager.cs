using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Requesters;

namespace JetBlack.Bloomberg
{
    public class IntraDayTickManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<TickerIntraDayTickData, TickerResponseError>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<TickerIntraDayTickData, TickerResponseError>>();

        public void Request(Session session, Service refDataService, IntradayTickRequester requester, Action<TickerIntraDayTickData> onSuccess, Action<TickerResponseError> onFailure)
        {
            var requests = requester.CreateRequests(refDataService);

            foreach (var request in requests)
            {
                var correlationId = new CorrelationID();
                _asyncHandlers.Add(correlationId, AsyncPattern.Create(onSuccess, onFailure));
                session.SendRequest(request, correlationId);
            }
        }

        public void ProcessIntradayTickResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<TickerIntraDayTickData, TickerResponseError> asyncHandler;
            if (!_asyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            var ticker = message.TopicName;

            if (!message.HasElement(ElementNames.ResponseError))
            {
                asyncHandler.OnFailure(new TickerResponseError(ticker, message.GetElement(ElementNames.ResponseError).ToResponseError()));
                return;
            }

            var tickData = message.GetElement("tickData");
            var tickDataArray = tickData.GetElement("tickData");
            var eids = ExtractEids(tickData.HasElement("eidData") ? tickData.GetElement("eidData") : null);

            var data = new List<IntradayTickData>();

            for (var i = 0; i < tickDataArray.NumValues; ++i)
            {
                var item = tickDataArray.GetValueAsElement(i);
                var conditionCodes = (item.HasElement("conditionCodes") ? item.GetElementAsString("conditionCodes").Split(',') : null);
                var exchangeCodes = (item.HasElement("exchangeCode") ? item.GetElementAsString("exchangeCode").Split(',') : null);

                data.Add(
                    new IntradayTickData(
                        item.GetElementAsDatetime("time").ToDateTime(),
                        (EventType)Enum.Parse(typeof(EventType), item.GetElementAsString("type"), true),
                        item.GetElementAsFloat64("value"),
                        item.GetElementAsInt32("size"),
                        conditionCodes,
                        exchangeCodes,
                        eids));
            }

            asyncHandler.OnSuccess(new TickerIntraDayTickData(ticker, data));
        }

        private static IList<int> ExtractEids(Element eidDataElement)
        {
            var eids = new List<int>();
            if (eidDataElement != null)
            {
                for (var i = 0; i < eidDataElement.NumValues; ++i)
                {
                    var eid = eidDataElement.GetValueAsInt32(i);
                    eids.Add(eid);
                }
            }
            return eids;
        }

    }
}
