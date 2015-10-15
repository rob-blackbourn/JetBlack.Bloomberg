using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class TickerData
    {
        public TickerData(string ticker, IDictionary<string, object> data, bool isPartialResponse)
        {
            Ticker = ticker;
            Data = data;
            IsPartialResponse = isPartialResponse;
        }

        public string Ticker { get; private set; }
        public IDictionary<string, object> Data { get; private set; }
        public bool IsPartialResponse { get; private set; }
    }
}
