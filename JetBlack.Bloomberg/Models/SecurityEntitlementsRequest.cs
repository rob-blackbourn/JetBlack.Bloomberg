using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class SecurityEntitlementsRequest
    {
        public SecurityEntitlementsRequest(IList<string> tickers)
        {
            Tickers = tickers;
        }

        public IList<string> Tickers { get; private set; } 
    }
}
