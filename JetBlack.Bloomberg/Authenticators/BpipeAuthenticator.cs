using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Managers;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Authenticators
{
    public class BpipeAuthenticator : Authenticator
    {
        private readonly ITokenProvider _tokenProvider;

        public BpipeAuthenticator(ITokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        public override IPromise<bool> Request(Session session, Service service, Identity identity)
        {
            return _tokenProvider.RequestToken().Then(token => Request(session, service, identity, token));
        }

        private IPromise<bool> Request(Session session, Service service, Identity identity, string token)
        {
            return new Promise<bool>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                AsyncHandlers.Add(correlationId, AsyncPattern<bool>.Create(resolve, reject));

                var request = CreateRequest(service, token);
                SendAuthorizationRequest(session, identity, request, correlationId);
            });
        }

        public override bool Authenticate(Session session, Service service, Identity identity)
        {
            var token = _tokenProvider.GenerateToken();
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
