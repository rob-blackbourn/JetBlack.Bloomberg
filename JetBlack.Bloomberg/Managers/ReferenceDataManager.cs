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
    internal class ReferenceDataManager : ResponseManager<ReferenceDataResponse>, IReferenceDataProvider
    {
        private readonly Service _service;
        private readonly Identity _identity;

        private readonly IDictionary<CorrelationID, ReferenceDataResponse> _partial = new Dictionary<CorrelationID, ReferenceDataResponse>();

        public ReferenceDataManager(Session session, Service service, Identity identity)
            : base(session)
        {
            _service = service;
            _identity = identity;
        }

        public IPromise<ReferenceDataResponse> Request(ReferenceDataRequest request)
        {
            return new Promise<ReferenceDataResponse>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                AsyncHandlers.Add(correlationId, AsyncPattern<ReferenceDataResponse>.Create(resolve, reject));
                Session.SendRequest(request.ToRequest(_service), _identity, correlationId);
            });
        }

        public void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<ReferenceDataResponse> asyncHandler;
            if (!AsyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                asyncHandler.OnFailure(new ContentException<ResponseError>(message.GetElement(ElementNames.ResponseError).ToResponseError()));
                return;
            }

            ReferenceDataResponse referenceDataResponse;
            if (_partial.TryGetValue(message.CorrelationID, out referenceDataResponse))
                _partial.Remove(message.CorrelationID);
            else
                referenceDataResponse = new ReferenceDataResponse(new Dictionary<string, TickerData>(new Dictionary<string, TickerData>()));

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

                TickerData tickerData;
                if (referenceDataResponse.ReferenceData.TryGetValue(ticker, out tickerData))
                    referenceDataResponse.ReferenceData.Remove(ticker);
                else
                    tickerData = new TickerData(ticker, new Dictionary<string, object>());

                var fields = security.GetElement(ElementNames.FieldData);
                for (var j = 0; j < fields.NumElements; ++j)
                {
                    var field = fields.GetElement(j);
                    var name = field.Name.ToString();
                    var value = field.GetFieldValue();
                    if (tickerData.Data.ContainsKey(name))
                        tickerData.Data[name] = value;
                    else
                        tickerData.Data.Add(name, value);
                }

                referenceDataResponse.ReferenceData[ticker] = tickerData;
            }

            if (isPartialResponse)
                _partial[message.CorrelationID] = referenceDataResponse;
            else
                asyncHandler.OnSuccess(referenceDataResponse);
        }
    }
}
