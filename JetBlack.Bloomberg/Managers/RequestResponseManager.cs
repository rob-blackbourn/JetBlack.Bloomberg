using System;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Managers
{
    internal abstract class RequestResponseManager<TRequest, TResponse> : ResponseManager<TResponse>, IResponseProcessor
    {
        protected RequestResponseManager(Session session)
            : base(session)
        {
        }

        public abstract bool CanProcessResponse(Message message);

        public abstract IObservable<TResponse> ToObservable(TRequest request);
    }
}
