using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Messages;

namespace JetBlack.Bloomberg
{
    public class TokenManager
    {
        private readonly IDictionary<CorrelationID, AsyncPattern<SessionEventArgs<TokenGenerationSuccessEventArgs>, SessionEventArgs<TokenGenerationFailureEventArgs>>> _tokenRequestHandlers = new Dictionary<CorrelationID, AsyncPattern<SessionEventArgs<TokenGenerationSuccessEventArgs>, SessionEventArgs<TokenGenerationFailureEventArgs>>>();

        public string GenerateToken(Session session)
        {
            var correlationId = new CorrelationID();
            var eventQueue = new EventQueue();
            session.GenerateToken(correlationId, eventQueue);
            var eventArgs = eventQueue.NextEvent();
            foreach (var message in eventArgs.GetMessages())
            {
                if (MessageTypeNames.TokenGenerationFailure.Equals(message.MessageType))
                    throw new Exception("Failed to generate token");
                if (MessageTypeNames.TokenGenerationSuccess.Equals(message.MessageType))
                    return message.GetElementAsString(ElementNames.Token);
            }
            throw new Exception("Token service failure.");
        }

        public void GenerateToken(Session session, Action<SessionEventArgs<TokenGenerationSuccessEventArgs>> onSuccess, Action<SessionEventArgs<TokenGenerationFailureEventArgs>> onFailure)
        {
            var correlationId = new CorrelationID();
            _tokenRequestHandlers.Add(correlationId, AsyncPattern.Create(onSuccess, onFailure));
            session.GenerateToken(correlationId);
        }

        public void ProcessTokenStatusEvent(Session session, Message message, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<SessionEventArgs<TokenGenerationSuccessEventArgs>, SessionEventArgs<TokenGenerationFailureEventArgs>> asyncPattern;
            if (!_tokenRequestHandlers.TryGetValue(message.CorrelationID, out asyncPattern))
                onFailure(session, message, new Exception("Failed to find correlation id: " + message.CorrelationID));
            else
            {
                _tokenRequestHandlers.Remove(message.CorrelationID);

                if (MessageTypeNames.TokenGenerationFailure.Equals(message.MessageType))
                {
                    var reason = message.GetElement(ElementNames.Reason);
                    asyncPattern.OnFailure(new SessionEventArgs<TokenGenerationFailureEventArgs>(session, reason.ToTokenGenerationFailureEventArgs()));
                }
                else if (MessageTypeNames.TokenGenerationSuccess.Equals(message.MessageType))
                    asyncPattern.OnSuccess(new SessionEventArgs<TokenGenerationSuccessEventArgs>(session, new TokenGenerationSuccessEventArgs(message.GetElementAsString(ElementNames.Token))));
                else
                    onFailure(session, message, new Exception("Unknown message type: " + message.MessageType));
            }
        }
    }
}
