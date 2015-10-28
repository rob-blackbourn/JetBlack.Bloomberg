using System;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Responses;

namespace JetBlack.Bloomberg
{
    public interface ISecurityEntitlementsProvider
    {
        IObservable<SecurityEntitlementsResponse> ToObservable(SecurityEntitlementsRequest securityEntitlementsRequest);
    }
}