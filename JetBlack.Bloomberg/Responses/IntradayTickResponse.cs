using System.Collections.Generic;

namespace JetBlack.Bloomberg.Responses
{
    public class IntradayTickResponse
    {
        public IntradayTickResponse(string ticker, IList<IntradayTick> intraDayTicks, IList<int> entitlementIds)
        {
            Ticker = ticker;
            IntraDayTicks = intraDayTicks;
            EntitlementIds = entitlementIds;
        }

        public string Ticker { get; private set; }
        public IList<IntradayTick> IntraDayTicks { get; private set; }
        public IList<int> EntitlementIds { get; private set; }
    }
}