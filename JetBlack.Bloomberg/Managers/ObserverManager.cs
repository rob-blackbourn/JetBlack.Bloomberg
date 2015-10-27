using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Managers
{
    internal class ObserverManager<TResponse> : Manager
    {
        protected readonly IDictionary<CorrelationID, IObserver<TResponse>> Observers = new Dictionary<CorrelationID, IObserver<TResponse>>();

        public ObserverManager(Session session) : base(session)
        {
        }
    }
}
