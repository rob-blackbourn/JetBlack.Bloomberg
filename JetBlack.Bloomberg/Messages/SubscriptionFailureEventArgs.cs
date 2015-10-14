using System;

namespace JetBlack.Bloomberg.Messages
{
    public class SubscriptionFailureEventArgs : EventArgs
    {
        public SubscriptionFailureEventArgs(string name, string source, string category, int errorCode, string description)
        {
            Name = name;
            Source = source;
            Category = category;
            ErrorCode = errorCode;
            Description = description;
        }

        public string Name { get; private set; }
        public string Source { get; private set; }
        public string Category { get; private set; }
        public int ErrorCode { get; private set; }
        public string Description { get; private set; }
    }
}
