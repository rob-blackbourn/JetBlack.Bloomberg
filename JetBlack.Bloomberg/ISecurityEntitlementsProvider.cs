using JetBlack.Bloomberg.Models;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface ISecurityEntitlementsProvider
    {
        IPromise<SecurityEntitlementsResponse> RequestSecurityEntitlements(SecurityEntitlementsRequest securityEntitlementsRequest);
    }
}