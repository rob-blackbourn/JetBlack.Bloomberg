using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Requesters;

namespace JetBlack.Bloomberg
{
    public class ReferenceDataManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<SessionEventArgs<DataReceivedEventArgs>, SessionEventArgs<ErrorResponseEventArgs>>> _referenceDataObservers = new Dictionary<CorrelationID, AsyncPattern<SessionEventArgs<DataReceivedEventArgs>, SessionEventArgs<ErrorResponseEventArgs>>>();

        public void Request(Session session, Service refDataService, ReferenceDataRequester requester, Action<SessionEventArgs<DataReceivedEventArgs>> onSuccess, Action<SessionEventArgs<ErrorResponseEventArgs>> onFailure)
        {
            var requests = requester.CreateRequests(refDataService);

            foreach (var request in requests)
            {
                var correlationId = new CorrelationID();
                _referenceDataObservers.Add(correlationId, AsyncPattern.Create(onSuccess, onFailure));
                session.SendRequest(request, correlationId);
            }
        }

        public void ProcessReferenceDataResponse(Session session, Message message, bool isPartialResponse)
        {
            AsyncPattern<SessionEventArgs<DataReceivedEventArgs>, SessionEventArgs<ErrorResponseEventArgs>> asyncHandler;
            if (!_referenceDataObservers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                return;
            }

            var securityData = new List<DataReceivedEventArgs>();
            //var securityErrors = new List<KeyValuePair<string, SecurityError>>();

            var securities = message.GetElement(ElementNames.SecurityData);
            for (var i = 0; i < securities.NumValues; ++i)
            {
                var security = securities.GetValueAsElement(i);
                var ticker = security.GetElementAsString(ElementNames.Security);

                if (security.HasElement(ElementNames.SecurityError))
                {
                    //securityErrors.Add(KeyValuePair.Create(ticker, new SecurityError(security.GetElement(ElementNames.SecurityError))));
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

                asyncHandler.OnSuccess(new SessionEventArgs<DataReceivedEventArgs>(session, new DataReceivedEventArgs(ticker, data)));
            }
        }
    }
}
