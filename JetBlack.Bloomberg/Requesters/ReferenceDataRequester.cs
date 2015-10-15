using System.Collections.Generic;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Requesters
{
    public class ReferenceDataRequester : Requester
    {
        public IEnumerable<string> Fields { get; set; }
        public IList<KeyValuePair<string, string>> Overrides { get; set; }
        public bool? ReturnEids { get; set; }
        public bool? ReturnFormattedValue { get; set; }
        public bool? UseUtcTime { get; set; }
        public bool? ForcedDelay { get; set; }
        public override bool MapTickers { get { return false; } }

        public override IEnumerable<Request> CreateRequests(Service refDataService)
        {
            var request = refDataService.CreateRequest("ReferenceDataRequest");

            foreach (var ticker in Tickers)
                request.Append("securities", ticker);

            foreach (var fieldMnemonic in Fields)
                request.Append("fields", fieldMnemonic);

            if (Overrides != null)
            {
                foreach (var pair in Overrides)
                {
                    var requestOverride = request["overrides"].AppendElement();
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
            if (UseUtcTime.HasValue)
                request.Set("useUTCTime", UseUtcTime.Value);

            return new[] { request };
        }
    }
}
