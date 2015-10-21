using System.Collections.Generic;
using JetBlack.Bloomberg.Models;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface ISecurityEntitlementsProvider
    {
        IPromise<ICollection<SecurityEntitlements>> RequestEntitlements(IEnumerable<string> tickers);
    }
}