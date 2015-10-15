using System.Collections.Generic;

namespace JetBlack.Bloomberg.Messages
{
    public class TickerIntradayBarData
    {
        public TickerIntradayBarData(string ticker, IList<IntradayBar> intradayBars, bool isPartialResponse)
        {
            Ticker = ticker;
            IntradayBars = intradayBars;
            IsPartialResponse = isPartialResponse;
        }

        public string Ticker { get; private set; }
        public IList<IntradayBar> IntradayBars { get; private set; }
        public bool IsPartialResponse { get; private set; }
    }
}