using System;
using Bloomberglp.Blpapi;
using JetBlack.Promises;

namespace JetBlack.Bloomberg.Authenticators
{
    public interface IAuthenticator
    {
        bool IsHandler(CorrelationID correlationId);
        void Process(Session session, Message message, Action<Session, Message, Exception> onFailure);
        IPromise<bool> Request(Session session, Service service);
        bool Authenticate(Session session, Service service);
        bool Permits(Element eidData, Service service);
    }
}
