using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Bloomberg.Utilities;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    internal class SecurityEntitlementsManager : ResponseManager<SecurityEntitlementsResponse>, ISecurityEntitlementsProvider
    {
        private readonly Service _service;
        private readonly Identity _identity;

        private readonly IDictionary<CorrelationID, IList<string>> _tickerMap = new Dictionary<CorrelationID, IList<string>>(); 
        private readonly IDictionary<CorrelationID, SecurityEntitlementsResponse> _partials = new Dictionary<CorrelationID, SecurityEntitlementsResponse>();

        public SecurityEntitlementsManager(Session session, Service service, Identity identity)
            : base(session)
        {
            _service = service;
            _identity = identity;
        }

        public IPromise<SecurityEntitlementsResponse> Request(SecurityEntitlementsRequest securityEntitlementsRequest)
        {
            return new Promise<SecurityEntitlementsResponse>((resolve, reject) =>
            {
                var request = _service.CreateRequest(OperationNames.SecurityEntitlementsRequest);
                var securitiesElement = request.GetElement(ElementNames.Securities);
                var securities = new List<string>();
                securityEntitlementsRequest.Tickers.ForEach(ticker =>
                {
                    securitiesElement.AppendValue(ticker);
                    securities.Add(ticker);
                });

                var correlationId = new CorrelationID();
                AsyncHandlers.Add(correlationId, new AsyncPattern<SecurityEntitlementsResponse>(resolve, reject));
                _tickerMap.Add(correlationId, securities);

                Session.SendRequest(request, _identity, correlationId);
            });
        }

        public void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<SecurityEntitlementsResponse> asyncHandler;
            if (!AsyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new ApplicationException("Failed to find handler"));
                return;
            }

            var tickers = _tickerMap[message.CorrelationID];
            _tickerMap.Remove(message.CorrelationID);

            SecurityEntitlementsResponse securityEntitlementsResponse;
            if (_partials.TryGetValue(message.CorrelationID, out securityEntitlementsResponse))
                _partials.Remove(message.CorrelationID);
            else
                securityEntitlementsResponse = new SecurityEntitlementsResponse(new Dictionary<string, SecurityEntitlements>());

            if (MessageTypeNames.SecurityEntitlementsResponse.Equals(message.MessageType))
            {
                var eidDataArrayElement = message.GetElement("eidData");
                for (var i = 0; i < eidDataArrayElement.NumValues; ++i)
                {
                    var ticker = tickers[i];
                    var eidDataElement = eidDataArrayElement.GetValueAsElement(i);

                    SecurityEntitlements securityEntitlements;
                    if (securityEntitlementsResponse.SecurityEntitlements.TryGetValue(ticker, out securityEntitlements))
                        securityEntitlementsResponse.SecurityEntitlements.Remove(ticker);
                    else
                    {
                        var status = eidDataElement.GetElementAsInt32("status");
                        var sequenceNumber = eidDataElement.GetElementAsInt32("sequenceNumber");
                        securityEntitlements = new SecurityEntitlements(ticker, status, sequenceNumber, new List<int>());
                    }

                    var eidsElement = eidDataElement.GetElement("eids");
                    for (var j = 0; j < eidsElement.NumValues; j++)
                        securityEntitlements.EntitlementIds.Add(eidsElement.GetValueAsInt32(j));
                }

                if (isPartialResponse)
                    _partials[message.CorrelationID] = securityEntitlementsResponse;
                else
                    asyncHandler.OnSuccess(securityEntitlementsResponse);
            }
            else
                onFailure(session, message, new Exception("Unknown message type: " + message));
        }
    }
}
