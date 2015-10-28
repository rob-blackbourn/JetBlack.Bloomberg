using System.Collections.Generic;

namespace JetBlack.Bloomberg.Responses
{
    public class IntradayBarResponse
    {
        public IntradayBarResponse(string ticker, IList<IntradayBar> intradayBars, IList<int> entitlementIds)
        {
            Ticker = ticker;
            IntradayBars = intradayBars;
            EntitlementIds = entitlementIds;
        }

        public string Ticker { get; private set; }
        public IList<IntradayBar> IntradayBars { get; private set; }
        public IList<int> EntitlementIds { get; private set; }
    }
}