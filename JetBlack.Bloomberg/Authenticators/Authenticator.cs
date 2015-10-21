using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Authenticators
{
    public abstract class Authenticator : IAuthenticator
    {
        protected readonly IDictionary<CorrelationID, AsyncPattern<bool>> AuthorizationRequestHandlers = new Dictionary<CorrelationID, AsyncPattern<bool>>();

        public abstract IPromise<bool> Request(Session session, Service service, Identity identity);

        public abstract bool Authenticate(Session session, Service service, Identity identity);

        protected bool Authenticate(Session session, Identity identity, Request request)
        {
            var correlationId = new CorrelationID();
            var eventQueue = new EventQueue();
            session.SendAuthorizationRequest(request, identity, eventQueue, correlationId);
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

        protected void SendAuthorizationRequest(Session session, Identity identity, Request request, CorrelationID correlationId)
        {
            session.SendAuthorizationRequest(request, identity, correlationId);
        }

        public bool IsHandler(CorrelationID correlationId)
        {
            return AuthorizationRequestHandlers.ContainsKey(correlationId);
        }

        public void Process(Session session, Message message, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<bool> asyncHandler;
            if (!AuthorizationRequestHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new ApplicationException("Failed to find handler"));
                return;
            }

            if (MessageTypeNames.AuthorizationFailure.Equals(message.MessageType))
                asyncHandler.OnSuccess(false);
            else if (MessageTypeNames.AuthorizationSuccess.Equals(message.MessageType))
                asyncHandler.OnSuccess(true);
            else
                onFailure(session, message, new Exception("Unknown message type: " + message));
        }

        public bool Permits(Service service, Identity identity, Element eidData)
        {
            if (eidData == null) return true;
            var missingEntitlements = new List<int>();
            return identity.HasEntitlements(eidData, service, missingEntitlements);
        }
    }
}
