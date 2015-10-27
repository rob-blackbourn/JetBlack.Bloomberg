using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class SubscriptionRequest
    {
        public SubscriptionRequest(string security, IList<string> fields)
        {
            Security = security;
            Fields = fields;
        }

        public string Security { get; private set; }
        public IList<string> Fields { get; private set; } 
    }
}
