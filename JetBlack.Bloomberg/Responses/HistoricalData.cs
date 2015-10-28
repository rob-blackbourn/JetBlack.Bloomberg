using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Responses
{
    public class HistoricalData : List<KeyValuePair<DateTime, IDictionary<string, object>>>
    {
    }
}