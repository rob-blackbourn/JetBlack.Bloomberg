using System.Collections.Generic;
using JetBlack.Bloomberg.Models;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface ISecurityEntitlementsManager
    {
        IPromise<ICollection<SecurityEntitlements>> RequestEntitlements(IEnumerable<string> tickers);
    }
}