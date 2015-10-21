using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface IIntradayTickProvider
    {
        IPromise<IntradayTickResponse> Request(IntradayTickRequest request);
    }
}