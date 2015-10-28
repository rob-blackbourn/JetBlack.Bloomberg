using System;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Responses;

namespace JetBlack.Bloomberg
{
    public interface IIntradayBarProvider
    {
        IObservable<IntradayBarResponse> ToObservable(IntradayBarRequest request);
    }
}