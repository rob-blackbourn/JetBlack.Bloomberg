using System;
using JetBlack.Bloomberg.Responses;

namespace JetBlack.Bloomberg
{
    public interface IUserEntitlementsProvider
    {
        IObservable<UserEntitlementsResponse> ToObservable(UserEntitlementsRequest userEntitlementsRequest);
    }
}
