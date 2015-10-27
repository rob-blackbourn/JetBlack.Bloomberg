using System;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;

namespace JetBlack.Bloomberg
{
    public interface IReferenceDataProvider
    {
        IObservable<ReferenceDataResponse> ToObservable(ReferenceDataRequest request);
    }
}