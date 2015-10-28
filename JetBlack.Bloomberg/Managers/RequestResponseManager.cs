using System;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Managers
{
    internal abstract class RequestResponseManager<TRequest, TResponse> : ResponseManager<TResponse>, IResponseProcessor
    {
        protected readonly Service Service;
        protected readonly Identity Identity;

        protected RequestResponseManager(Session session, Service service, Identity identity)
            : base(session)
        {
            Service = service;
            Identity = identity;
        }

        public abstract bool CanProcessResponse(Message message);

        public abstract IObservable<TResponse> ToObservable(TRequest request);
    }
}
