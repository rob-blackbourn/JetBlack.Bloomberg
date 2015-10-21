using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface IIntradayBarProvider
    {
        IPromise<IntradayBarResponse> RequestIntradayBar(IntradayBarRequest request);
    }
}