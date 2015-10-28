using System.Collections.Generic;

namespace JetBlack.Bloomberg.Responses
{
    public class UserEntitlementsResponse
    {
        public UserEntitlementsResponse(IList<int> entitlementIds)
        {
            EntitlementIds = entitlementIds;
        }

        public IList<int> EntitlementIds { get; private set; }
    }
}
