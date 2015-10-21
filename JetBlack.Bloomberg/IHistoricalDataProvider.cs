using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface IHistoricalDataProvider
    {
        IPromise<HistoricalDataResponse> Request(HistoricalDataRequest request);
    }
}