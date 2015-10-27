using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class HistoricalData : List<KeyValuePair<DateTime, IDictionary<string, object>>>
    {
    }
}