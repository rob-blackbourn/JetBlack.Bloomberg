using Bloomberglp.Blpapi;
using System.Collections.Generic;

namespace JetBlack.Bloomberg
{
    public class ReferenceDataRequester : Requester
    {
        public IEnumerable<string> Fields { get; set; }
        public IList<KeyValuePair<string, string>> Overrides { get; set; }
        public bool? ReturnEids { get; set; }
        public bool? ReturnFormattedValue { get; set; }
        public bool? UseUTCTime { get; set; }
        public bool? ForcedDelay { get; set; }
        public override bool MapTickers { get { return false; } }

        public override IEnumerable<Request> CreateRequests(Service refDataService)
        {
            Request request = refDataService.CreateRequest("ReferenceDataRequest");

            foreach (string ticker in Tickers)
                request.Append("securities", ticker);

            foreach (string fieldMnemonic in Fields)
                request.Append("fields", fieldMnemonic);

            if (Overrides != null)
            {
                foreach (var pair in Overrides)
                {
                    Element requestOverride = request["overrides"].AppendElement();
                    requestOverride.SetElement("fieldId", pair.Key);
                    requestOverride.SetElement("value", pair.Value);
                }
            }

            if (ForcedDelay.HasValue)
                request.Set("forcedDelay", ForcedDelay.Value);
            if (ReturnEids.HasValue)
                request.Set("returnEids", ReturnEids.Value);
            if (ReturnFormattedValue.HasValue)
                request.Set("returnFormattedValue", ReturnFormattedValue.Value);
            if (UseUTCTime.HasValue)
                request.Set("useUTCTime", UseUTCTime.Value);

            return new[] { request };
        }
    }
}
