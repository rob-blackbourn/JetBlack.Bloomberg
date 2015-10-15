using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requesters;

namespace JetBlack.Bloomberg
{
    public class ReferenceDataManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<TickerData, TickerSecurityError>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<TickerData, TickerSecurityError>>();

        public void Request(Session session, Service refDataService, ReferenceDataRequester requester, Action<TickerData> onSuccess, Action<TickerSecurityError> onFailure)
        {
            var requests = requester.CreateRequests(refDataService);

            foreach (var request in requests)
            {
                var correlationId = new CorrelationID();
                _asyncHandlers.Add(correlationId, AsyncPattern.Create(onSuccess, onFailure));
                session.SendRequest(request, correlationId);
            }
        }

        public void ProcessReferenceDataResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<TickerData, TickerSecurityError> asyncHandler;
            if (!_asyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                return;
            }

            var securities = message.GetElement(ElementNames.SecurityData);
            for (var i = 0; i < securities.NumValues; ++i)
            {
                var security = securities.GetValueAsElement(i);
                var ticker = security.GetElementAsString(ElementNames.Security);


                if (security.HasElement(ElementNames.SecurityError))
                {
                    asyncHandler.OnFailure(new TickerSecurityError(ticker, security.GetElement(ElementNames.SecurityError).ToSecurityError(), isPartialResponse));
                    continue;
                }

                var data = new Dictionary<string, object>();
                var fields = security.GetElement(ElementNames.FieldData);
                for (var j = 0; j < fields.NumElements; ++j)
                {
                    var field = fields.GetElement(j);
                    var name = field.Name.ToString();
                    var value = field.GetFieldValue();
                    if (data.ContainsKey(name))
                        data[name] = value;
                    else
                        data.Add(name, value);
                }

                asyncHandler.OnSuccess(new TickerData(ticker, data, isPartialResponse));
            }
        }
    }
}
