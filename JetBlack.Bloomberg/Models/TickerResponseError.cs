namespace JetBlack.Bloomberg.Models
{
    public class TickerResponseError
    {
        public TickerResponseError(string ticker, ResponseError responseError)
        {
            Ticker = ticker;
            ResponseError = responseError;
        }

        public string Ticker { get; private set; }
        public ResponseError ResponseError { get; private set; }
    }
}