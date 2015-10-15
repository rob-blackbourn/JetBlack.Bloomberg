using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Promises;

namespace JetBlack.Bloomberg.Authenticators
{
    public class BpipeAuthenticator : Authenticator
    {
        private readonly TokenManager _tokenManager;

        public BpipeAuthenticator(Identity identity, TokenManager tokenManager)
            : base(identity)
        {
            _tokenManager = tokenManager;
        }

        public override IPromise<bool> Request(Session session, Service service)
        {
            return _tokenManager.Request(session).Then(token => Request(session, service, token));
        }

        private IPromise<bool> Request(Session session, Service service, string token)
        {
            return new Promise<bool>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                AuthorizationRequestHandlers.Add(correlationId, AsyncPattern<bool>.Create(resolve, reject));

                var request = CreateRequest(service, token);
                SendAuthorizationRequest(session, request, correlationId);
            });
        }

        public override bool Authenticate(Session session, Service service)
        {
            var token = _tokenManager.GenerateToken(session);
            var request = CreateRequest(service, token);
            return Authenticate(session, request);
        }

        private static Request CreateRequest(Service service, string token)
        {
            var request = service.CreateAuthorizationRequest();
            request.Set(ElementNames.Token, token);
            return request;
        }
    }
}
