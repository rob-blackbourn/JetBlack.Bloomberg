using System.Collections.Generic;

namespace JetBlack.Bloomberg.Models
{
    public class ReferenceDataResponse
    {
        public ReferenceDataResponse(IDictionary<string,TickerData> referenceData)
        {
            ReferenceData = referenceData;
        }

        public IDictionary<string,TickerData> ReferenceData { get; private set; }
    }
}
