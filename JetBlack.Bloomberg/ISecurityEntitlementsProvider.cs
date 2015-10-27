using System;
using JetBlack.Bloomberg.Models;

namespace JetBlack.Bloomberg
{
    public interface ISecurityEntitlementsProvider
    {
        IObservable<SecurityEntitlementsResponse> ToObservable(SecurityEntitlementsRequest securityEntitlementsRequest);
    }
}