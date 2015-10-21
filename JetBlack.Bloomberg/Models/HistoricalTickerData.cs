using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class HistoricalTickerData
    {
        public HistoricalTickerData(string ticker, IList<KeyValuePair<DateTime, IDictionary<string, object>>> data)
        {
            Ticker = ticker;
            Data = data;
        }

        public string Ticker { get; private set; }
        public IList<KeyValuePair<DateTime, IDictionary<string, object>>> Data { get; private set; }
    }
}