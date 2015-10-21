using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Patterns;

namespace JetBlack.Bloomberg.Managers
{
    internal class AsyncManager<TResponse> : Manager
    {
        protected readonly IDictionary<CorrelationID, AsyncPattern<TResponse>> AsyncHandlers = new Dictionary<CorrelationID, AsyncPattern<TResponse>>();

        public AsyncManager(Session session) : base(session)
        {
        }
    }
}
