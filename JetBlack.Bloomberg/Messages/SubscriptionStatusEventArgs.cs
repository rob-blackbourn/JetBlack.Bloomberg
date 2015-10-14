using System;

namespace JetBlack.Bloomberg.Messages
{
    public class SubscriptionStatusEventArgs : EventArgs
    {
        public SubscriptionStatusEventArgs(string name, SubscriptionStatus subscriptionStatus, string source, string category, int errorCode, string description)
        {
            Name = name;
            SubscriptionStatus = subscriptionStatus;
            Source = source;
            Category = category;
            ErrorCode = errorCode;
            Description = description;
        }

        public string Name { get; private set; }
        public SubscriptionStatus SubscriptionStatus { get; private set; }
        public string Source { get; private set; }
        public string Category { get; private set; }
        public int ErrorCode { get; private set; }
        public string Description { get; private set; }
    }
}
