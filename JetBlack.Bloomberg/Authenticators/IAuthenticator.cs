using System;
using Bloomberglp.Blpapi;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Authenticators
{
    public interface IAuthenticator
    {
        bool IsHandler(CorrelationID correlationId);
        IPromise<bool> Request(Session session, Service service, Identity identity);
        bool Authenticate(Session session, Service service, Identity identity);
        bool Permits(Service service, Identity identity, Element eidData);

        bool CanProcessResponse(Message message);
        void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure);
    }
}
