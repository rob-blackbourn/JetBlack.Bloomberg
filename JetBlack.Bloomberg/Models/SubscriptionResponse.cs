namespace JetBlack.Bloomberg.Models
{
    public class SubscriptionResponse
    {
        public SubscriptionResponse(TickerData tickerData)
        {
            TickerData = tickerData;
        }

        public TickerData TickerData { get; private set; }
    }
}
