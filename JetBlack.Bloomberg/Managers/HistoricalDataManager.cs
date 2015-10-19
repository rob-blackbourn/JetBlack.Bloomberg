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
    public class HistoricalDataManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>>>();
        private readonly IDictionary<CorrelationID, IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>> _partial = new Dictionary<CorrelationID, IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>>();

        public IPromise<IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>> Request(Session session, Identity identity, Service refDataService, HistoricalDataRequest request)
        {
            return new Promise<IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                _asyncHandlers.Add(correlationId, AsyncPattern<IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>>.Create(resolve, reject));
                session.SendRequest(request.Create(refDataService), identity, correlationId);
            });
        }

        public void Process(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>> asyncHandler;
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

            IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>> historicalTickerDataMap;
            if (_partial.TryGetValue(message.CorrelationID, out historicalTickerDataMap))
                _partial.Remove(message.CorrelationID);
            else
                historicalTickerDataMap = new Dictionary<string, IDictionary<DateTime, IDictionary<string, object>>>();

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

                IDictionary<DateTime, IDictionary<string, object>> historicalTickerData;
                if (historicalTickerDataMap.TryGetValue(ticker, out historicalTickerData))
                    historicalTickerDataMap.Remove(ticker);
                else
                    historicalTickerData = new Dictionary<DateTime,IDictionary<string,object>>();

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
                        historicalTickerData.Add(date, data);
                    }
                }

                historicalTickerDataMap[ticker] = historicalTickerData;
            }

            if (isPartialResponse)
                _partial[message.CorrelationID] = historicalTickerDataMap;
            else
                asyncHandler.OnSuccess(historicalTickerDataMap);
        }
    }
}
