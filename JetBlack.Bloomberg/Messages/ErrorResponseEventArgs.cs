using System;

namespace JetBlack.Bloomberg.Messages
{
    public class ErrorResponseEventArgs : EventArgs
    {
        public ErrorResponseEventArgs(string name, string messageType, string responseError)
        {
            Name = name;
            MessageType = messageType;
            ResponseError = responseError;
        }

        public string Name { get; private set; }
        public string MessageType { get; private set; }
        public string ResponseError { get; private set; }
    }
}
