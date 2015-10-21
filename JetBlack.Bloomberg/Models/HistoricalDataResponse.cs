using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class HistoricalDataResponse
    {
        public HistoricalDataResponse(IDictionary<string, HistoricalTickerData> historicalTickerData)
        {
            HistoricalTickerData = historicalTickerData;
        }

        public IDictionary<string, HistoricalTickerData> HistoricalTickerData { get; private set; }
    }
}
