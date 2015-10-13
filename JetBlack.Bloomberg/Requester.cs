using Bloomberglp.Blpapi;
using System.Collections.Generic;

namespace JetBlack.Bloomberg
{
    public abstract class Requester
    {
        public ICollection<string> Tickers { get; set; }
        public abstract bool MapTickers { get; }

        public abstract IEnumerable<Request> CreateRequests(Service refDataService);
    }
}
