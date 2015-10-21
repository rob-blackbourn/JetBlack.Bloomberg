using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Managers;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Authenticators
{
    public class BpipeAuthenticator : Authenticator
    {
        private readonly TokenManager _tokenManager;

        public BpipeAuthenticator(TokenManager tokenManager)
        {
            _tokenManager = tokenManager;
        }

        public override IPromise<bool> Request(Session session, Service service, Identity identity)
        {
            return _tokenManager.Request(session).Then(token => Request(session, service, identity, token));
        }

        private IPromise<bool> Request(Session session, Service service, Identity identity, string token)
        {
            return new Promise<bool>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                AuthorizationRequestHandlers.Add(correlationId, AsyncPattern<bool>.Create(resolve, reject));

                var request = CreateRequest(service, token);
                SendAuthorizationRequest(session, identity, request, correlationId);
            });
        }

        public override bool Authenticate(Session session, Service service, Identity identity)
        {
            var token = _tokenManager.GenerateToken(session);
            var request = CreateRequest(service, token);
            return Authenticate(session, identity, request);
        }

        private static Request CreateRequest(Service service, string token)
        {
            var request = service.CreateAuthorizationRequest();
            request.Set(ElementNames.Token, token);
            return request;
        }
    }
}
