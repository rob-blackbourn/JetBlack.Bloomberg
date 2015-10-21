using System;
using System.Collections.Generic;
using JetBlack.Bloomberg.Requests;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface IHistoricalDataProvider
    {
        IPromise<IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>> RequestHistoricalData(HistoricalDataRequest request);
    }
}