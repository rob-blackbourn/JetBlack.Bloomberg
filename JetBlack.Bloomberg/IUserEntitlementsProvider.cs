using System;
using JetBlack.Bloomberg.Models;

namespace JetBlack.Bloomberg
{
    public interface IUserEntitlementsProvider
    {
        IObservable<UserEntitlementsResponse> ToObservable(UserEntitlementsRequest userEntitlementsRequest);
    }
}
