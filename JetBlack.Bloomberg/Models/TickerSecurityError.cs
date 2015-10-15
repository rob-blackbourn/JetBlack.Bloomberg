namespace JetBlack.Bloomberg.Models
{
    public class TickerSecurityError
    {
        public TickerSecurityError(string ticker, SecurityError securityError, bool isPartialResponse)
        {
            Ticker = ticker;
            SecurityError = securityError;
            IsPartialResponse = isPartialResponse;
        }

        public string Ticker { get; private set; }
        public SecurityError SecurityError { get; private set; }
        public bool IsPartialResponse { get; private set; }
    }
}