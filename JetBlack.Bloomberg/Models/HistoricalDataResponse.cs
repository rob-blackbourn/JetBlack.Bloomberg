using System.Collections.Generic;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Models
{
    public class HistoricalDataResponse : Dictionary<string, Either<ResponseError, HistoricalData>>
    {
    }
}
