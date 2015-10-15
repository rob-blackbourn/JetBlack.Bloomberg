using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Messages
{
    public class HistoricalTickerData
    {
        public HistoricalTickerData(string ticker, IDictionary<DateTime, IDictionary<string, object>> data, bool isPartialResponse)
        {
            Ticker = ticker;
            Data = data;
            IsPartialResponse = isPartialResponse;
        }

        public string Ticker { get; private set; }
        public IDictionary<DateTime, IDictionary<string, object>> Data { get; private set; }
        public bool IsPartialResponse { get; private set; }
    }
}