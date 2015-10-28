using System;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Bloomberg.Responses;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    internal class TokenManager : AsyncManager<string>, ITokenProvider
    {
        public TokenManager(Session session)
            : base(session)
        {
        }

        public string GenerateToken()
        {
            var correlationId = new CorrelationID();
            var eventQueue = new EventQueue();
            Session.GenerateToken(correlationId, eventQueue);
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
                AsyncHandlers.Add(correlationId, AsyncPattern<string>.Create(resolve, reject));
                Session.GenerateToken(correlationId);
            });
        }

        public void ProcessTokenStatusEvent(Session session, Message message, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<string> asyncPattern;
            if (!AsyncHandlers.TryGetValue(message.CorrelationID, out asyncPattern))
                onFailure(session, message, new Exception("Failed to find correlation id: " + message.CorrelationID));
            else
            {
                AsyncHandlers.Remove(message.CorrelationID);

                if (MessageTypeNames.TokenGenerationFailure.Equals(message.MessageType))
                {
                    var reasonElement = message.GetElement(ElementNames.Reason);

                    var error = new ResponseError(
                        reasonElement.GetElementAsString(ElementNames.Source),
                        reasonElement.GetElementAsString(ElementNames.Category),
                        reasonElement.GetElementAsString(ElementNames.SubCategory),
                        reasonElement.GetElementAsInt32(ElementNames.ErrorCode),
                        reasonElement.GetElementAsString(ElementNames.Description));

                    asyncPattern.OnFailure(new ContentException<ResponseError>(error));
                }
                else if (MessageTypeNames.TokenGenerationSuccess.Equals(message.MessageType))
                    asyncPattern.OnSuccess(message.GetElementAsString(ElementNames.Token));
                else
                    onFailure(session, message, new Exception("Unknown message type: " + message.MessageType));
            }
        }
    }
}
