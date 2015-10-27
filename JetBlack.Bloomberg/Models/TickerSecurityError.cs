namespace JetBlack.Bloomberg.Models
{
    public class TickerSecurityError
    {
        public TickerSecurityError(string ticker, SecurityError securityError)
        {
            Ticker = ticker;
            SecurityError = securityError;
        }

        public string Ticker { get; private set; }
        public SecurityError SecurityError { get; private set; }
    }
}