using System.Collections.Generic;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Models
{
    public class HistoricalDataResponse
    {
        public HistoricalDataResponse(IDictionary<string, Either<SecurityError,HistoricalTickerData>> historicalTickerData)
        {
            HistoricalTickerData = historicalTickerData;
        }

        public IDictionary<string, Either<SecurityError, HistoricalTickerData>> HistoricalTickerData { get; private set; }
    }
}
