using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Requesters;

namespace JetBlack.Bloomberg
{
    public class HistoricalDataManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<HistoricalTickerData, TickerSecurityError>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<HistoricalTickerData, TickerSecurityError>>();

        public void Request(Session session, Service refDataService, HistoricalDataRequester requester, Action<HistoricalTickerData> onSuccess, Action<TickerSecurityError> onFailure)
        {
            var requests = requester.CreateRequests(refDataService);

            foreach (var request in requests)
            {
                var correlationId = new CorrelationID();
                _asyncHandlers.Add(correlationId, AsyncPattern.Create(onSuccess, onFailure));
                session.SendRequest(request, correlationId);
            }
        }

        public void ProcessHistoricalDataResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<HistoricalTickerData, TickerSecurityError> asyncHandler;
            if (!_asyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                return;
            }

            var securityDataArray = message.GetElement(ElementNames.SecurityData);

            for (var i = 0; i < securityDataArray.NumValues; ++i)
            {
                var securityData = securityDataArray.GetElement(i);
                var ticker = securityData.GetValueAsString();

                if (securityDataArray.HasElement("securityError"))
                {
                    asyncHandler.OnFailure(new TickerSecurityError(ticker, securityDataArray.GetElement("securityError").ToSecurityError(), isPartialResponse));
                    continue;
                }

                var fieldDataArray = securityDataArray.GetElement(ElementNames.FieldData);

                var data = new Dictionary<DateTime, IDictionary<string, object>>();

                for (var j = 0; j < fieldDataArray.NumValues; ++j)
                {
                    var messageWrapper = new Dictionary<string, object>();
                    var fieldData = fieldDataArray.GetValueAsElement(j);

                    for (var k = 0; k < fieldData.NumElements; ++k)
                    {
                        var field = fieldData.GetElement(k);
                        var name = field.Name.ToString();
                        var value = field.GetFieldValue();
                        if (messageWrapper.ContainsKey(name))
                            messageWrapper[name] = value;
                        else
                            messageWrapper.Add(name, value);
                    }

                    if (messageWrapper.ContainsKey("date"))
                    {
                        var date = (DateTime)messageWrapper["date"];
                        messageWrapper.Remove("date");
                        data.Add(date, messageWrapper);
                    }
                }

                asyncHandler.OnSuccess(new HistoricalTickerData(ticker, data, isPartialResponse));
            }
        }
    }
}
