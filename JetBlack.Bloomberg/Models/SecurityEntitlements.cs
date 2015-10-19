using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class SecurityEntitlements
    {
        public SecurityEntitlements(string security, int status, int sequenceNumber, IList<int> entitlementIds)
        {
            Security = security;
            Status = status;
            SequenceNumber = sequenceNumber;
            EntitlementIds = entitlementIds;
        }

        public string Security { get; private set; }
        public int Status { get; private set; }
        public int SequenceNumber { get; private set; }
        public IList<int> EntitlementIds { get; private set; }
    }
}
