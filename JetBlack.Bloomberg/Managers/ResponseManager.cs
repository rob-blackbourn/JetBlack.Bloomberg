using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Patterns;

namespace JetBlack.Bloomberg.Managers
{
    internal class ResponseManager<TResponse> : Manager
    {
        protected readonly IDictionary<CorrelationID, AsyncPattern<TResponse>> AsyncHandlers = new Dictionary<CorrelationID, AsyncPattern<TResponse>>();

        public ResponseManager(Session session)
            : base(session)
        {
        }
    }
}
