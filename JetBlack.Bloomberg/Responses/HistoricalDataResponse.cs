using System.Collections.Generic;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Responses
{
    public class HistoricalDataResponse : Dictionary<string, Either<ResponseError, HistoricalData>>
    {
    }
}
