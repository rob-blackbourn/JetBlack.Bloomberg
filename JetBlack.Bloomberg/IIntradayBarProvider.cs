using System;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;

namespace JetBlack.Bloomberg
{
    public interface IIntradayBarProvider
    {
        IObservable<IntradayBarResponse> ToObservable(IntradayBarRequest request);
    }
}