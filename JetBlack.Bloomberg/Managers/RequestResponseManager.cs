using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Managers
{
    internal abstract class RequestResponseManager<TRequest, TResponse, TCorrelation> : ObserverManager<TResponse>, IResponseProcessor
    {
        private readonly IDictionary<CorrelationID, TCorrelation> _correlationMap = new Dictionary<CorrelationID, TCorrelation>();

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

        public abstract void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure);

        public void Add(CorrelationID correlationId, IObserver<TResponse> observer, TCorrelation correlation)
        {
            Add(correlationId, observer);
            _correlationMap.Add(correlationId, correlation);
        }

        public override void Remove(CorrelationID correlationId)
        {
            base.Remove(correlationId);
            _correlationMap.Remove(correlationId);
        }

        public bool TryGet(CorrelationID correlationId, out IObserver<TResponse> observer, out TCorrelation correlation)
        {
            if (!TryGet(correlationId, out observer))
            {
                correlation = default (TCorrelation);
                return false;
            }

            return _correlationMap.TryGetValue(correlationId, out correlation);
        }
    }
}
