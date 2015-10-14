using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Messages
{
    public class HistoricalDataReceivedEventArgs : EventArgs
    {
        public HistoricalDataReceivedEventArgs(string name, IDictionary<DateTime, IDictionary<string, object>> historicalDataMessage)
        {
            Name = name;
            HistoricalDataMessage = historicalDataMessage;
        }

        public string Name { get; private set; }
        public IDictionary<DateTime, IDictionary<string, object>> HistoricalDataMessage { get; private set; }
    }
}
