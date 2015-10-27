using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Patterns;

namespace JetBlack.Bloomberg.Managers
{
    internal abstract class AsyncManager<TResponse> : Manager
    {
        protected readonly IDictionary<CorrelationID, AsyncPattern<TResponse>> AsyncHandlers = new Dictionary<CorrelationID, AsyncPattern<TResponse>>();

        protected AsyncManager(Session session)
            : base(session)
        {
        }
    }
}
