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

        public override void RequestAuthentication(Session session, Service service, Action<SessionEventArgs<AuthorizationSuccessEventArgs>> onSuccess, Action<SessionEventArgs<AuthorizationFailureEventArgs>> onFailure)
        {
            _tokenManager.GenerateToken(
                session,
                tokenSuccessArgs =>
                {
                    var correlationId = new CorrelationID();
                    AuthorizationRequestHandlers.Add(correlationId, AsyncPattern.Create(onSuccess, onFailure));

                    var request = CreateRequest(service, tokenSuccessArgs.EventArgs.Token);
                    SendAuthorizationRequest(session, request, correlationId);
                },
                tokenFailureArgs => onFailure(new SessionEventArgs<AuthorizationFailureEventArgs>(session, new AuthorizationFailureEventArgs())));
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
