using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;

namespace JetBlack.Bloomberg.Requesters
{
    public class ReferenceDataRequestFactory : RequestFactory
    {
        public IEnumerable<string> Fields { get; set; }
        public IList<KeyValuePair<string, string>> Overrides { get; set; }
        public bool? ReturnEids { get; set; }
        public bool? ReturnFormattedValue { get; set; }
        public bool? UseUtcTime { get; set; }
        public bool? ForcedDelay { get; set; }

        public override IEnumerable<Request> CreateRequests(Service refDataService)
        {
            var request = refDataService.CreateRequest(OperationNames.ReferenceDataRequest);

            foreach (var ticker in Tickers)
                request.Append(ElementNames.Securities, ticker);

            foreach (var fieldMnemonic in Fields)
                request.Append(ElementNames.Fields, fieldMnemonic);

            if (Overrides != null)
            {
                foreach (var pair in Overrides)
                {
                    var requestOverride = request[ElementNames.Overrides].AppendElement();
                    requestOverride.SetElement(ElementNames.FieldId, pair.Key);
                    requestOverride.SetElement(ElementNames.Value, pair.Value);
                }
            }

            if (ForcedDelay.HasValue)
                request.Set(ElementNames.ForcedDelay, ForcedDelay.Value);
            if (ReturnEids.HasValue)
                request.Set(ElementNames.ReturnEids, ReturnEids.Value);
            if (ReturnFormattedValue.HasValue)
                request.Set(ElementNames.ReturnFormattedValue, ReturnFormattedValue.Value);
            if (UseUtcTime.HasValue)
                request.Set(ElementNames.UseUTCTime, UseUtcTime.Value);

            return new[] { request };
        }
    }
}
