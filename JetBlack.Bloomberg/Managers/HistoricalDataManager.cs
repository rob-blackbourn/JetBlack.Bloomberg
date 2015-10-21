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
    internal class HistoricalDataManager : IHistoricalDataProvider
    {
        private readonly Session _session;
        private readonly Service _service;
        private readonly Identity _identity;

        private readonly IDictionary<CorrelationID, AsyncPattern<HistoricalDataResponse>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<HistoricalDataResponse>>();
        private readonly IDictionary<CorrelationID, HistoricalDataResponse> _partial = new Dictionary<CorrelationID, HistoricalDataResponse>();

        public HistoricalDataManager(Session session, Service service, Identity identity)
        {
            _session = session;
            _service = service;
            _identity = identity;
        }

        public IPromise<HistoricalDataResponse> RequestHistoricalData(HistoricalDataRequest request)
        {
            return new Promise<HistoricalDataResponse>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                _asyncHandlers.Add(correlationId, AsyncPattern<HistoricalDataResponse>.Create(resolve, reject));
                _session.SendRequest(request.ToRequest(_service), _identity, correlationId);
            });
        }

        public void Process(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<HistoricalDataResponse> asyncHandler;
            if (!_asyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                asyncHandler.OnFailure(new ContentException<ResponseError>(message.GetElement(ElementNames.ResponseError).ToResponseError()));
                return;
            }

            HistoricalDataResponse historicalTickerDataMap;
            if (_partial.TryGetValue(message.CorrelationID, out historicalTickerDataMap))
                _partial.Remove(message.CorrelationID);
            else
                historicalTickerDataMap = new HistoricalDataResponse(new Dictionary<string,HistoricalTickerData>());

            var securityDataArray = message.GetElement(ElementNames.SecurityData);

            for (var i = 0; i < securityDataArray.NumValues; ++i)
            {
                var securityData = securityDataArray.GetElement(i);
                var ticker = securityData.GetValueAsString();

                if (securityDataArray.HasElement(ElementNames.SecurityError))
                {
                    asyncHandler.OnFailure(new ContentException<TickerSecurityError>(new TickerSecurityError(ticker, securityDataArray.GetElement(ElementNames.SecurityError).ToSecurityError(), isPartialResponse)));
                    continue;
                }

                HistoricalTickerData historicalTickerData;
                if (historicalTickerDataMap.HistoricalTickerData.TryGetValue(ticker, out historicalTickerData))
                    historicalTickerDataMap.HistoricalTickerData.Remove(ticker);
                else
                    historicalTickerData = new HistoricalTickerData(ticker, new Dictionary<DateTime, IDictionary<string, object>>());

                var fieldDataArray = securityDataArray.GetElement(ElementNames.FieldData);

                for (var j = 0; j < fieldDataArray.NumValues; ++j)
                {
                    var data = new Dictionary<string, object>();
                    var fieldData = fieldDataArray.GetValueAsElement(j);

                    for (var k = 0; k < fieldData.NumElements; ++k)
                    {
                        var field = fieldData.GetElement(k);
                        var name = field.Name.ToString();
                        var value = field.GetFieldValue();
                        if (data.ContainsKey(name))
                            data[name] = value;
                        else
                            data.Add(name, value);
                    }

                    if (data.ContainsKey("date"))
                    {
                        var date = (DateTime)data["date"];
                        data.Remove("date");
                        historicalTickerData.Data.Add(date, data);
                    }
                }

                historicalTickerDataMap.HistoricalTickerData[ticker] = historicalTickerData;
            }

            if (isPartialResponse)
                _partial[message.CorrelationID] = historicalTickerDataMap;
            else
                asyncHandler.OnSuccess(historicalTickerDataMap);
        }
    }
}
