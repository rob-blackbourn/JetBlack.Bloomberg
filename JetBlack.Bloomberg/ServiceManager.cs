using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Promises;

namespace JetBlack.Bloomberg
{
    public class ServiceManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<Service>> _openHandlers = new Dictionary<CorrelationID, AsyncPattern<Service>>();

        public Service Open(Session session, string uri)
        {
            session.OpenService(uri);
            return session.GetService(uri);
        }

        public IPromise<Service> Request(Session session, string uri)
        {
            return new Promise<Service>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                _openHandlers.Add(correlationId, AsyncPattern<Service>.Create(resolve, reject));
                session.OpenServiceAsync(uri, correlationId);
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