using System;
using System.Collections.Generic;
using System.Net;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;

namespace JetBlack.Bloomberg.Authenticators
{
    public class SapiAuthenticator : Authenticator
    {
        private readonly IPAddress _clientIpAddress;
        private readonly string _uuid;
        private readonly IDictionary<CorrelationID, AsyncPattern<SessionEventArgs<AuthorizationSuccessEventArgs>, SessionEventArgs<AuthorizationFailureEventArgs>>> _authorizationRequestHandlers = new Dictionary<CorrelationID, AsyncPattern<SessionEventArgs<AuthorizationSuccessEventArgs>, SessionEventArgs<AuthorizationFailureEventArgs>>>();

        public SapiAuthenticator(Identity identity, IPAddress clientIpAddress, string uuid)
            : base(identity)
        {
            _clientIpAddress = clientIpAddress;
            _uuid = uuid;
        }

        public override void RequestAuthentication(Session session, Service service, Action<SessionEventArgs<AuthorizationSuccessEventArgs>> onSuccess, Action<SessionEventArgs<AuthorizationFailureEventArgs>> onFailure)
        {
            var correlationId = new CorrelationID();
            AuthorizationRequestHandlers.Add(correlationId, AsyncPattern.Create(onSuccess, onFailure));

            var request = CreateRequest(service, _clientIpAddress, _uuid);
            SendAuthorizationRequest(session, request, correlationId);
        }

        public override bool Authenticate(Session session, Service service)
        {
            var request = CreateRequest(service, _clientIpAddress, _uuid);
            return Authenticate(session, request);
        }

        private static Request CreateRequest(Service service, IPAddress clientIpAddress, string uuid)
        {
            var request = service.CreateAuthorizationRequest();
            request.Set("uuid", uuid);
            request.Set("ipAddress", clientIpAddress.ToString());
            return request;
        }
    }
}
