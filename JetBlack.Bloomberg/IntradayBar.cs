using System;

namespace JetBlack.Bloomberg
{
    public class IntradayBar
    {
        public DateTime Time { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public int NumEvents { get; set; }
        public long Volume { get; set; }
    }
}
