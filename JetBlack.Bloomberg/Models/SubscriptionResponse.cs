using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class SubscriptionResponse
    {
        public SubscriptionResponse(string ticker, IDictionary<string, object> data)
        {
            Data = data;
            Ticker = ticker;
        }

        public string Ticker { get; private set; }
        public IDictionary<string, object> Data { get; private set; }
    }
}
