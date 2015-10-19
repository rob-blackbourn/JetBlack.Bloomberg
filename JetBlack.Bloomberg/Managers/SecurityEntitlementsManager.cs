using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Bloomberg.Utilities;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    public class SecurityEntitlementsManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<ICollection<SecurityEntitlements>>> _asyncHandlers = new Dictionary<CorrelationID, AsyncPattern<ICollection<SecurityEntitlements>>>();
        private readonly IDictionary<CorrelationID, IList<string>> _tickerMap = new ConcurrentDictionary<CorrelationID, IList<string>>(); 
        private readonly IDictionary<CorrelationID, IDictionary<string, SecurityEntitlements>> _partials = new ConcurrentDictionary<CorrelationID, IDictionary<string, SecurityEntitlements>>();

        public IPromise<ICollection<SecurityEntitlements>> RequestEntitlements(Session session, Service service, Identity identity, IEnumerable<string> tickers)
        {
            return new Promise<ICollection<SecurityEntitlements>>((resolve, reject) =>
            {
                var request = service.CreateRequest(OperationNames.SecurityEntitlementsRequest);
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

                session.SendRequest(request, identity, correlationId);
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
