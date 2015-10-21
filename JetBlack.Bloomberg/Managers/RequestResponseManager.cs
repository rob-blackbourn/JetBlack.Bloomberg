using Bloomberglp.Blpapi;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    internal abstract class RequestResponseManager<TRequest, TResponse> : ResponseManager<TResponse>
    {
        protected RequestResponseManager(Session session)
            : base(session)
        {
        }

        public abstract IPromise<TResponse> Request(TRequest request);
    }
}
