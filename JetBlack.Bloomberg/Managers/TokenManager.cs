using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Bloomberg.Utilities;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    internal class TokenManager : ITokenProvider
    {
        private readonly Session _session;

        private readonly IDictionary<CorrelationID, AsyncPattern<string>> _tokenRequestHandlers = new Dictionary<CorrelationID, AsyncPattern<string>>();

        public TokenManager(Session session)
        {
            _session = session;
        }

        public string GenerateToken()
        {
            var correlationId = new CorrelationID();
            var eventQueue = new EventQueue();
            _session.GenerateToken(correlationId, eventQueue);
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

        public IPromise<string> RequestToken()
        {
            return new Promise<string>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                _tokenRequestHandlers.Add(correlationId, AsyncPattern<string>.Create(resolve, reject));
                _session.GenerateToken(correlationId);
            });
        }

        public void ProcessTokenStatusEvent(Session session, Message message, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<string> asyncPattern;
            if (!_tokenRequestHandlers.TryGetValue(message.CorrelationID, out asyncPattern))
                onFailure(session, message, new Exception("Failed to find correlation id: " + message.CorrelationID));
            else
            {
                _tokenRequestHandlers.Remove(message.CorrelationID);

                if (MessageTypeNames.TokenGenerationFailure.Equals(message.MessageType))
                {
                    var reason = message.GetElement(ElementNames.Reason);
                    asyncPattern.OnFailure(new ContentException<TokenGenerationFailure>(reason.ToTokenGenerationFailureEventArgs()));
                }
                else if (MessageTypeNames.TokenGenerationSuccess.Equals(message.MessageType))
                    asyncPattern.OnSuccess(message.GetElementAsString(ElementNames.Token));
                else
                    onFailure(session, message, new Exception("Unknown message type: " + message.MessageType));
            }
        }
    }
}
