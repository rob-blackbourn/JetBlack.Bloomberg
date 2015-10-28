using System;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;

namespace JetBlack.Bloomberg
{
    public interface ISecurityEntitlementsProvider
    {
        IObservable<SecurityEntitlementsResponse> ToObservable(SecurityEntitlementsRequest securityEntitlementsRequest);
    }
}