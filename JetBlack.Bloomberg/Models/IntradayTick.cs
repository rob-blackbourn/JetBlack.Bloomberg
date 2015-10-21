using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class IntradayTick
    {
        public IntradayTick(DateTime time, EventType eventType, double value, int size, IList<string> conditionCodes, IList<string> exchangeCodes)
        {
            ExchangeCodes = exchangeCodes;
            ConditionCodes = conditionCodes;
            Size = size;
            Value = value;
            EventType = eventType;
            Time = time;
        }

        public DateTime Time { get; private set; }
        public EventType EventType { get; private set; }
        public double Value { get; private set; }
        public int Size { get; private set; }
        public IList<string> ConditionCodes { get; private set; }
        public IList<string> ExchangeCodes { get; private set; }

        public override string ToString()
        {
            var conditionsCodes = ConditionCodes == null ? string.Empty : string.Join(",", new List<string>(ConditionCodes).ToArray());
            var exchangeCodes = ExchangeCodes == null ? string.Empty : string.Join(",", (new List<string>(ExchangeCodes)).ToArray());
            return string.Format("Time={0}, EventType={1}, Value={2}, Size={3}, Condition Codes={4}, Exchange Codes={5}",
                Time.ToString("yyyy-MM-dd hh:mm:ss"), EventType, Value, Size, conditionsCodes, exchangeCodes);
        }
    }
}
