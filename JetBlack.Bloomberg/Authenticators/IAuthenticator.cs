using System;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;

namespace JetBlack.Bloomberg.Authenticators
{
    public interface IAuthenticator
    {
        bool IsHandler(CorrelationID correlationId);
        void Process(Session session, Message message, Action<Session, Message, Exception> onFailure);
        void RequestAuthentication(Session session, Service service, Action<SessionDecorator<AuthorizationSuccessEventArgs>> onSuccess, Action<SessionDecorator<AuthorizationFailureEventArgs>> onFailure);
        bool Authenticate(Session session, Service service);
        bool Permits(Element eidData, Service service);
    }
}
