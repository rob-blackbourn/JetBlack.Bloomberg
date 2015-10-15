using System.Collections.Generic;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Requesters
{
    public abstract class Requester
    {
        public ICollection<string> Tickers { get; set; }
        public abstract bool MapTickers { get; }

        public abstract IEnumerable<Request> CreateRequests(Service refDataService);
    }
}
