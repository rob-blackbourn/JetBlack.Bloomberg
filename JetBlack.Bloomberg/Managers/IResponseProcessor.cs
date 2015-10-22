using System;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Managers
{
    internal interface IResponseProcessor
    {
        bool CanProcessResponse(Message message);
        void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure);
    }
}