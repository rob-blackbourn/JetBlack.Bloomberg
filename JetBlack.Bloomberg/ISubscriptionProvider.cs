using System;
using System.Collections.Generic;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Responses;

namespace JetBlack.Bloomberg
{
    public interface ISubscriptionProvider
    {
        IObservable<SubscriptionResponse> ToObservable(IEnumerable<SubscriptionRequest> request);
    }
}