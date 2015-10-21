using System;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    internal class ServiceManager : AsyncManager<Service>
    {
        public ServiceManager(Session session)
            : base(session)
        {
        }

        public Service Open(string uri)
        {
            Session.OpenService(uri);
            return Session.GetService(uri);
        }

        public IPromise<Service> Request(string uri)
        {
            return new Promise<Service>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                AsyncHandlers.Add(correlationId, AsyncPattern<Service>.Create(resolve, reject));
                Session.OpenServiceAsync(uri, correlationId);
            });
        }

        public void Process(Session session, Message message, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<Service> asyncHandler;
            if (!AsyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Failed to find handler for service status event with correlation id: " + message.CorrelationID));
                return;
            }

            AsyncHandlers.Remove(message.CorrelationID);

            if (MessageTypeNames.ServiceOpenFailure.Equals(message.MessageType))
                asyncHandler.OnFailure(new ContentException<ServiceOpenFailureEventArgs>(new ServiceOpenFailureEventArgs()));
            else if (MessageTypeNames.ServiceOpened.Equals(message.MessageType))
                asyncHandler.OnSuccess(session.GetService(message.GetElementAsString(ElementNames.ServiceName)));
            else
                onFailure(session, message, new Exception("Unknown service status event message: " + message));
        }
    }
}