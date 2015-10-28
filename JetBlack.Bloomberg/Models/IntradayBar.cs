using System;

namespace JetBlack.Bloomberg.Models
{
    public class IntradayBar
    {
        public IntradayBar(DateTime time, double open, double high, double low, double close, int numEvents, long volume, double value)
        {
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            NumEvents = numEvents;
            Volume = volume;
            Value = value;
        }

        public DateTime Time { get; private set; }
        public double Open { get; private set; }
        public double High { get; private set; }
        public double Low { get; private set; }
        public double Close { get; private set; }
        public int NumEvents { get; private set; }
        public long Volume { get; private set; }
        public double Value { get; private set; }

        public override string ToString()
        {
            return string.Format("Time={0}, Open={1}, High={2}, Low={3}, Close={4}, NumEvents={5}, Volume={6}, Value={7}", Time, Open, High, Low, Close, NumEvents, Volume, Value);
        }
    }
}
