using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class TickerIntradayTickData
    {
        public TickerIntradayTickData(string ticker, IList<IntradayTickData> intraDayTicks, IList<int> entitlementIds)
        {
            Ticker = ticker;
            IntraDayTicks = intraDayTicks;
            EntitlementIds = entitlementIds;
        }

        public string Ticker { get; private set; }
        public IList<IntradayTickData> IntraDayTicks { get; private set; }
        public IList<int> EntitlementIds { get; private set; }
    }
}