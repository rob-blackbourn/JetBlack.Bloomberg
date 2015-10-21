using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface IIntradayBarProvider
    {
        IPromise<TickerIntradayBarData> RequestIntradayBar(IntradayBarRequest request);
    }
}