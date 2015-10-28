using System;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Responses;

namespace JetBlack.Bloomberg
{
    public interface IReferenceDataProvider
    {
        IObservable<ReferenceDataResponse> ToObservable(ReferenceDataRequest request);
    }
}