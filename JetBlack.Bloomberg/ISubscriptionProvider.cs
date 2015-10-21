using System;
using System.Collections.Generic;
using JetBlack.Bloomberg.Models;

namespace JetBlack.Bloomberg
{
    public interface ISubscriptionProvider
    {
        IObservable<TickerData> ToObservable(IEnumerable<string> tickers, IEnumerable<string> fields);
    }
}