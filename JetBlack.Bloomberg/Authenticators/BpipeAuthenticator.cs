using System;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;

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

        public override void RequestAuthentication(Session session, Service service, Action<SessionDecorator<AuthorizationSuccessEventArgs>> onSuccess, Action<SessionDecorator<AuthorizationFailureEventArgs>> onFailure)
        {
            _tokenManager.GenerateToken(
                session,
                tokenSuccessArgs =>
                {
                    var correlationId = new CorrelationID();
                    AuthorizationRequestHandlers.Add(correlationId, AsyncPattern.Create(onSuccess, onFailure));

                    var request = CreateRequest(service, tokenSuccessArgs.Content.Token);
                    SendAuthorizationRequest(session, request, correlationId);
                },
                tokenFailureArgs => onFailure(new SessionDecorator<AuthorizationFailureEventArgs>(session, new AuthorizationFailureEventArgs())));
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
