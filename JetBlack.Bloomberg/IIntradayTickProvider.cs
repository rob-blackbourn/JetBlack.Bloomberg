using System;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;

namespace JetBlack.Bloomberg
{
    public interface IIntradayTickProvider
    {
        IObservable<IntradayTickResponse> ToObservable(IntradayTickRequest request);
    }
}