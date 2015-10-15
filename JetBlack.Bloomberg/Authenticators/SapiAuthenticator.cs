using System;
using System.Net;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Patterns;

namespace JetBlack.Bloomberg.Authenticators
{
    public class SapiAuthenticator : Authenticator
    {
        private readonly IPAddress _clientIpAddress;
        private readonly string _uuid;

        public SapiAuthenticator(Identity identity, IPAddress clientIpAddress, string uuid)
            : base(identity)
        {
            _clientIpAddress = clientIpAddress;
            _uuid = uuid;
        }

        public override void RequestAuthentication(Session session, Service service, Action<SessionDecorator<AuthorizationSuccessEventArgs>> onSuccess, Action<SessionDecorator<AuthorizationFailureEventArgs>> onFailure)
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
