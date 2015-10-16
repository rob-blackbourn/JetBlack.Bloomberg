using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class TickerIntradayBarData
    {
        public TickerIntradayBarData(string ticker, IList<IntradayBar> intradayBars, IList<int> entitlementIds, bool isPartialResponse)
        {
            Ticker = ticker;
            IntradayBars = intradayBars;
            EntitlementIds = entitlementIds;
            IsPartialResponse = isPartialResponse;
        }

        public string Ticker { get; private set; }
        public IList<IntradayBar> IntradayBars { get; private set; }
        public IList<int> EntitlementIds { get; private set; }
        public bool IsPartialResponse { get; private set; }
    }
}