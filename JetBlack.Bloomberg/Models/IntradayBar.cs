using System;

namespace JetBlack.Bloomberg.Models
{
    public class IntradayBar
    {
        public IntradayBar(DateTime time, double open, double high, double low, double close, int numEvents, long volume)
        {
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            NumEvents = numEvents;
            Volume = volume;
        }

        public DateTime Time { get; private set; }
        public double Open { get; private set; }
        public double High { get; private set; }
        public double Low { get; private set; }
        public double Close { get; private set; }
        public int NumEvents { get; private set; }
        public long Volume { get; private set; }
    }
}
