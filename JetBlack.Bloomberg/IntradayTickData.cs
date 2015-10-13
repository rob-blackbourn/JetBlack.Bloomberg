using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg
{
    public class IntradayTickData
    {
        public DateTime Time { get; set; }
        public EventType EventType { get; set; }
        public double Value { get; set; }
        public int Size { get; set; }
        public IList<string> ConditionCodes { get; set; }
        public IList<string> ExchangeCodes { get; set; }
        public IList<int> EidData { get; set; }

        public override string ToString()
        {
            string conditionsCodes = ConditionCodes == null ? string.Empty : string.Join(",", new List<string>(ConditionCodes).ToArray());
            string exchangeCodes = ExchangeCodes == null ? string.Empty : string.Join(",", (new List<string>(ExchangeCodes)).ToArray());
            return string.Format("Time={0}, EventType={1}, Value={2}, Size={3}, Condition Codes={4}, Exchange Codes={5}",
                Time.ToString("yyyy-MM-dd hh:mm:ss"), EventType, Value, Size, conditionsCodes, exchangeCodes);
        }
    }
}
