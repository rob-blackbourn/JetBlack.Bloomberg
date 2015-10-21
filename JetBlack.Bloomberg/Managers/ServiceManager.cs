using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    internal class ServiceManager
    {
        private readonly Session _session;

        private readonly IDictionary<CorrelationID, AsyncPattern<Service>> _openHandlers = new Dictionary<CorrelationID, AsyncPattern<Service>>();

        public ServiceManager(Session session)
        {
            _session = session;
        }

        public Service Open(string uri)
        {
            _session.OpenService(uri);
            return _session.GetService(uri);
        }

        public IPromise<Service> Request(string uri)
        {
            return new Promise<Service>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                _openHandlers.Add(correlationId, AsyncPattern<Service>.Create(resolve, reject));
                _session.OpenServiceAsync(uri, correlationId);
            });
        }

        public void Process(Session session, Message message, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<Service> asyncHandler;
            if (!_openHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Failed to find handler for service status event with correlation id: " + message.CorrelationID));
                return;
            }

            _openHandlers.Remove(message.CorrelationID);

            if (MessageTypeNames.ServiceOpenFailure.Equals(message.MessageType))
                asyncHandler.OnFailure(new ContentException<ServiceOpenFailureEventArgs>(new ServiceOpenFailureEventArgs()));
            else if (MessageTypeNames.ServiceOpened.Equals(message.MessageType))
                asyncHandler.OnSuccess(session.GetService(message.GetElementAsString(ElementNames.ServiceName)));
            else
                onFailure(session, message, new Exception("Unknown service status event message: " + message));
        }
    }
}