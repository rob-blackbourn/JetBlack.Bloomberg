using System;

namespace JetBlack.Bloomberg.Messages
{
    public class FieldSubscriptionStatusEventArgs : EventArgs
    {
        public FieldSubscriptionStatusEventArgs(string name, string fieldId, FieldSubscriptionStatus fieldSubscriptionStatus, string source, string category, string subCategory, int errorCode, string description)
        {
            Name = name;
            FieldId = fieldId;
            FieldSubscriptionStatus = fieldSubscriptionStatus;
            Source = source;
            Category = category;
            SubCategory = subCategory;
            ErrorCode = errorCode;
            Description = description;
        }

        public string Name { get; private set; }
        public string FieldId { get; private set; }
        public FieldSubscriptionStatus FieldSubscriptionStatus { get; private set; }
        public string Source { get; private set; }
        public string Category { get; private set; }
        public string SubCategory { get; private set; }
        public int ErrorCode { get; private set; }
        public string Description { get; private set; }
    }
}
