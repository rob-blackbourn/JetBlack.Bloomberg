using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class SecurityEntitlementsResponse
    {
        public SecurityEntitlementsResponse(IDictionary<string, SecurityEntitlements> securityEntitlements)
        {
            SecurityEntitlements = securityEntitlements;
        }

        public IDictionary<string, SecurityEntitlements> SecurityEntitlements { get; private set; }
    }
}
