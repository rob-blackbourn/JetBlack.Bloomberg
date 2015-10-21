using JetBlack.Bloomberg.Models;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface ISecurityEntitlementsProvider
    {
        IPromise<SecurityEntitlementsResponse> Request(SecurityEntitlementsRequest securityEntitlementsRequest);
    }
}