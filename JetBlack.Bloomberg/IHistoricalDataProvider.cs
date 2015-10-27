using System;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;

namespace JetBlack.Bloomberg
{
    public interface IHistoricalDataProvider
    {
        IObservable<HistoricalDataResponse> ToObservable(HistoricalDataRequest request);
    }
}