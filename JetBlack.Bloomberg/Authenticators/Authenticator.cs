using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;

namespace JetBlack.Bloomberg.Authenticators
{
    public abstract class Authenticator : IAuthenticator
    {
        private readonly Identity _identity;
        protected readonly IDictionary<CorrelationID, AsyncPattern<SessionEventArgs<AuthorizationSuccessEventArgs>, SessionEventArgs<AuthorizationFailureEventArgs>>> AuthorizationRequestHandlers = new Dictionary<CorrelationID, AsyncPattern<SessionEventArgs<AuthorizationSuccessEventArgs>, SessionEventArgs<AuthorizationFailureEventArgs>>>();

        protected Authenticator(Identity identity)
        {
            _identity = identity;
        }

        protected bool Authenticate(Session session, Request request)
        {
            var correlationId = new CorrelationID();
            var eventQueue = new EventQueue();
            session.SendAuthorizationRequest(request, _identity, eventQueue, correlationId);
            while (true)
            {
                var eventArgs = eventQueue.NextEvent();

                foreach (var message in eventArgs.GetMessages())
                {
                    if (MessageTypeNames.AuthorizationFailure.Equals(message.MessageType))
                        return false;

                    if (MessageTypeNames.AuthorizationSuccess.Equals(message.MessageType))
                        return true;

                    throw new Exception("Unknown message type: " + message);
                }
            }
        }

        protected void SendAuthorizationRequest(Session session, Request request, CorrelationID correlationId)
        {
            session.SendAuthorizationRequest(request, _identity, correlationId);
        }

        public bool IsHandler(CorrelationID correlationId)
        {
            return AuthorizationRequestHandlers.ContainsKey(correlationId);
        }

        public void Process(Session session, Message message, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<SessionEventArgs<AuthorizationSuccessEventArgs>, SessionEventArgs<AuthorizationFailureEventArgs>> asyncHandler;
            if (AuthorizationRequestHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                if (MessageTypeNames.AuthorizationFailure.Equals(message.MessageType))
                    asyncHandler.OnFailure(new SessionEventArgs<AuthorizationFailureEventArgs>(session, new AuthorizationFailureEventArgs()));
                else if (MessageTypeNames.AuthorizationSuccess.Equals(message.MessageType))
                    asyncHandler.OnSuccess(new SessionEventArgs<AuthorizationSuccessEventArgs>(session, new AuthorizationSuccessEventArgs()));
                else
                    onFailure(session, message, new Exception("Unknown message type: " + message));
            }
        }

        public bool Permits(Element eidData, Service service)
        {
            if (eidData == null) return true;
            var missingEntitlements = new List<int>();
            return _identity.HasEntitlements(eidData, service, missingEntitlements);
        }

        public abstract void RequestAuthentication(Session session, Service service, Action<SessionEventArgs<AuthorizationSuccessEventArgs>> onSuccess, Action<SessionEventArgs<AuthorizationFailureEventArgs>> onFailure);

        public abstract bool Authenticate(Session session, Service service);
    }
}
