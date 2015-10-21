using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface IReferenceDataProvider
    {
        IPromise<ReferenceDataResponse> Request(ReferenceDataRequest request);
    }
}