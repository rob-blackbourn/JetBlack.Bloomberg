using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class TickerData
    {
        public TickerData(string ticker, IDictionary<string, object> data)
        {
            Ticker = ticker;
            Data = data;
        }

        public string Ticker { get; private set; }
        public IDictionary<string, object> Data { get; private set; }
    }
}
