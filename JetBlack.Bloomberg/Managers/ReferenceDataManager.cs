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
    internal class ReferenceDataManager : IReferenceDataProvider
    {
        private readonly Session _session;
        private readonly Service _service;
        private readonly Identity _identity;

        private readonly IDictionary<CorrelationID, AsyncPattern<IDictionary<string,IDictionary<string,object>>>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<IDictionary<string,IDictionary<string,object>>>>();
        private readonly IDictionary<CorrelationID, IDictionary<string, IDictionary<string, object>>> _partial = new Dictionary<CorrelationID, IDictionary<string, IDictionary<string, object>>>();

        public ReferenceDataManager(Session session, Service service, Identity identity)
        {
            _session = session;
            _service = service;
            _identity = identity;
        }

        public IPromise<IDictionary<string, IDictionary<string, object>>> RequestReferenceData(ReferenceDataRequest request)
        {
            return new Promise<IDictionary<string, IDictionary<string, object>>>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                _asyncHandlers.Add(correlationId, AsyncPattern<IDictionary<string, IDictionary<string, object>>>.Create(resolve, reject));
                _session.SendRequest(request.ToRequest(_service), _identity, correlationId);
            });
        }

        public void Process(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<IDictionary<string,IDictionary<string,object>>> asyncHandler;
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

            IDictionary<string, IDictionary<string, object>> tickerDataMap;
            if (_partial.TryGetValue(message.CorrelationID, out tickerDataMap))
                _partial.Remove(message.CorrelationID);
            else
                tickerDataMap = new Dictionary<string, IDictionary<string, object>>();

            var securities = message.GetElement(ElementNames.SecurityData);
            for (var i = 0; i < securities.NumValues; ++i)
            {
                var security = securities.GetValueAsElement(i);
                var ticker = security.GetElementAsString(ElementNames.Security);
                
                if (security.HasElement(ElementNames.SecurityError))
                {
                    asyncHandler.OnFailure(new ContentException<TickerSecurityError>(new TickerSecurityError(ticker, security.GetElement(ElementNames.SecurityError).ToSecurityError(), isPartialResponse)));
                    continue;
                }

                IDictionary<string, object> data;
                if (tickerDataMap.TryGetValue(ticker, out data))
                    tickerDataMap.Remove(ticker);
                else
                    data = new Dictionary<string, object>();

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

                tickerDataMap[ticker] = data;
            }

            if (isPartialResponse)
                _partial[message.CorrelationID] = tickerDataMap;
            else
                asyncHandler.OnSuccess(tickerDataMap);
        }
    }
}
