using System;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Responses;

namespace JetBlack.Bloomberg
{
    public interface IHistoricalDataProvider
    {
        IObservable<HistoricalDataResponse> ToObservable(HistoricalDataRequest request);
    }
}