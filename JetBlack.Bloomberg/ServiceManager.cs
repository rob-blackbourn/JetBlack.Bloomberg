using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;

namespace JetBlack.Bloomberg
{
    public class ServiceManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<SessionEventArgs<ServiceOpenedEventArgs>, SessionEventArgs<ServiceOpenFailureEventArgs>>> _openHandlers = new Dictionary<CorrelationID, AsyncPattern<SessionEventArgs<ServiceOpenedEventArgs>, SessionEventArgs<ServiceOpenFailureEventArgs>>>();

        public Service Open(Session session, string uri)
        {
            session.OpenService(uri);
            return session.GetService(uri);
        }

        public void Open(Session session, string uri, Action<SessionEventArgs<ServiceOpenedEventArgs>> onSuccess, Action<SessionEventArgs<ServiceOpenFailureEventArgs>> onFailure)
        {
            var correlationId = new CorrelationID();
            _openHandlers.Add(correlationId, AsyncPattern.Create(onSuccess, onFailure));
            session.OpenServiceAsync(uri, correlationId);
        }

        public void ProcessServiceStatusEvent(Session session, Message message, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<SessionEventArgs<ServiceOpenedEventArgs>, SessionEventArgs<ServiceOpenFailureEventArgs>> asyncHandler;
            if (!_openHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
                onFailure(session, message, new Exception("Failed to find handler for service status event with correlation id: " + message.CorrelationID));
            else
            {
                _openHandlers.Remove(message.CorrelationID);

                if (MessageTypeNames.ServiceOpenFailure.Equals(message.MessageType))
                    asyncHandler.OnFailure(new SessionEventArgs<ServiceOpenFailureEventArgs>(session, new ServiceOpenFailureEventArgs()));
                else if (MessageTypeNames.ServiceOpened.Equals(message.MessageType))
                    asyncHandler.OnSuccess(new SessionEventArgs<ServiceOpenedEventArgs>(session, new ServiceOpenedEventArgs(session.GetService(message.GetElementAsString(ElementNames.ServiceName)))));
                else
                    onFailure(session, message, new Exception("Unknown service status event message: " + message));
            }
        }
    }
}