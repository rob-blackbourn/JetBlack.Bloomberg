using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface IIntradayTickProvider
    {
        IPromise<TickerIntradayTickData> RequestIntradayTick(IntradayTickRequest request);
    }
}