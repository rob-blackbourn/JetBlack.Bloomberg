using System.Collections.Generic;

namespace JetBlack.Bloomberg.Messages
{
    public class ReferenceDataResponse
    {
        public ReferenceDataResponse(IList<TickerData> referenceData, bool isPartialResponse)
        {
            ReferenceData = referenceData;
            IsPartialResponse = isPartialResponse;
        }

        public IList<TickerData> ReferenceData { get; private set; }
        public bool IsPartialResponse { get; private set; }
    }
}
