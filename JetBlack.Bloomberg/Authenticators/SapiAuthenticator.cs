using System.Net;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Authenticators
{
    public class SapiAuthenticator : Authenticator
    {
        private readonly IPAddress _clientIpAddress;
        private readonly string _uuid;

        public SapiAuthenticator(IPAddress clientIpAddress, string uuid)
        {
            _clientIpAddress = clientIpAddress;
            _uuid = uuid;
        }

        public override IPromise<bool> Request(Session session, Service service, Identity identity)
        {
            return new Promise<bool>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                AuthorizationRequestHandlers.Add(correlationId, AsyncPattern<bool>.Create(resolve, reject));

                var request = CreateRequest(service, _clientIpAddress, _uuid);
                SendAuthorizationRequest(session, identity, request, correlationId);
            });
        }

        public override bool Authenticate(Session session, Service service, Identity identity)
        {
            var request = CreateRequest(service, _clientIpAddress, _uuid);
            return Authenticate(session, identity, request);
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
