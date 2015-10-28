using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Managers
{
    internal class ObserverManager<TResponse> : Manager
    {
        private readonly IDictionary<CorrelationID, IObserver<TResponse>> _observers = new Dictionary<CorrelationID, IObserver<TResponse>>();
        protected readonly Identity Identity;

        public ObserverManager(Session session, Identity identity) : base(session)
        {
            Identity = identity;
        }

        public void Add(CorrelationID correlationId, IObserver<TResponse> observer)
        {
            _observers.Add(correlationId, observer);
        }

        public virtual void Remove(CorrelationID correlationId)
        {
            _observers.Remove(correlationId);
        }

        public bool TryGet(CorrelationID correlationId, out IObserver<TResponse> observer)
        {
            return _observers.TryGetValue(correlationId, out observer);
        }
    }
}
