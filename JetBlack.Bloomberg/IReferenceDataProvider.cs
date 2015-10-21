using System.Collections.Generic;
using JetBlack.Bloomberg.Requests;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface IReferenceDataProvider
    {
        IPromise<IDictionary<string, IDictionary<string, object>>> RequestReferenceData(ReferenceDataRequest request);
    }
}