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
    internal class SecurityEntitlementsManager : ISecurityEntitlementsManager
    {
        private readonly Session _session;
        private readonly Service _service;
        private readonly Identity _identity;

        private readonly IDictionary<CorrelationID, AsyncPattern<ICollection<SecurityEntitlements>>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<ICollection<SecurityEntitlements>>>();
        private readonly IDictionary<CorrelationID, IList<string>> _tickerMap = new Dictionary<CorrelationID, IList<string>>(); 
        private readonly IDictionary<CorrelationID, IDictionary<string, SecurityEntitlements>> _partials = new Dictionary<CorrelationID, IDictionary<string, SecurityEntitlements>>();

        public SecurityEntitlementsManager(Session session, Service service, Identity identity)
        {
            _session = session;
            _service = service;
            _identity = identity;
        }

        public IPromise<ICollection<SecurityEntitlements>> RequestEntitlements(IEnumerable<string> tickers)
        {
            return new Promise<ICollection<SecurityEntitlements>>((resolve, reject) =>
            {
                var request = _service.CreateRequest(OperationNames.SecurityEntitlementsRequest);
                var securitiesElement = request.GetElement(ElementNames.Securities);
                var securities = new List<string>();
                tickers.ForEach(ticker =>
                {
                    securitiesElement.AppendValue(ticker);
                    securities.Add(ticker);
                });

                var correlationId = new CorrelationID();
                _asyncHandlers.Add(correlationId, new AsyncPattern<ICollection<SecurityEntitlements>>(resolve, reject));
                _tickerMap.Add(correlationId, securities);

                _session.SendRequest(request, _identity, correlationId);
            });
        }

        public void Process(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<ICollection<SecurityEntitlements>> asyncHandler;
            if (!_asyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new ApplicationException("Failed to find handler"));
                return;
            }

            var tickers = _tickerMap[message.CorrelationID];
            _tickerMap.Remove(message.CorrelationID);

            IDictionary<string, SecurityEntitlements> securityEntitlementsMap;
            if (_partials.TryGetValue(message.CorrelationID, out securityEntitlementsMap))
                _partials.Remove(message.CorrelationID);
            else
                securityEntitlementsMap = new Dictionary<string, SecurityEntitlements>();

            if (MessageTypeNames.SecurityEntitlementsResponse.Equals(message.MessageType))
            {
                var eidDataArrayElement = message.GetElement("eidData");
                for (var i = 0; i < eidDataArrayElement.NumValues; ++i)
                {
                    var ticker = tickers[i];
                    var eidDataElement = eidDataArrayElement.GetValueAsElement(i);

                    SecurityEntitlements securityEntitlements;
                    if (securityEntitlementsMap.TryGetValue(ticker, out securityEntitlements))
                        securityEntitlementsMap.Remove(ticker);
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
                    _partials[message.CorrelationID] = securityEntitlementsMap;
                else
                    asyncHandler.OnSuccess(securityEntitlementsMap.Values);
            }
            else
                onFailure(session, message, new Exception("Unknown message type: " + message));
        }
    }
}
