using System.Collections.Generic;

namespace JetBlack.Bloomberg.Messages
{
    public class TickerIntraDayTickData
    {
        public TickerIntraDayTickData(string ticker, IList<IntradayTickData> intraDayTicks)
        {
            Ticker = ticker;
            IntraDayTicks = intraDayTicks;
        }

        public string Ticker { get; private set; }
        public IList<IntradayTickData> IntraDayTicks { get; private set; }
    }
}