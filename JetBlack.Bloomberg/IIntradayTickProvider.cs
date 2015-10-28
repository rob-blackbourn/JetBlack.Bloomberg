using System;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Responses;

namespace JetBlack.Bloomberg
{
    public interface IIntradayTickProvider
    {
        IObservable<IntradayTickResponse> ToObservable(IntradayTickRequest request);
    }
}