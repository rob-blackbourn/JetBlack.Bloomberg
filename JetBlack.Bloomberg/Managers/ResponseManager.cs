using System;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Managers
{
    internal abstract class ResponseManager<TResponse> : AsyncManager<TResponse>
    {
        protected ResponseManager(Session session)
            : base(session)
        {
        }

        public abstract void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure);
    }
}
