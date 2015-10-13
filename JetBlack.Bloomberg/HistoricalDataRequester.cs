using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg
{
    public class HistoricalDataRequester : Requester
    {
        public IList<string> Fields { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public PeriodicitySelection PeriodicitySelection { get; set; }
        public PeriodicityAdjustment PeriodicityAdjustment { get; set; }
        public string Currency { get; set; }
        public OverrideOption? OverrideOption { get; set; }
        public PricingOption? PricingOption { get; set; }
        public NonTradingDayFillOption? NonTradingDayFillOption { get; set; }
        public NonTradingDayFillMethod? NonTradingDayFillMethod { get; set; }
        public int? MaxDataPoints { get; set; }
        public bool? ReturnEids { get; set; }
        public bool? ReturnRelativeDate { get; set; }
        public bool? AdjustmentNormal { get; set; }
        public bool? AdjustmentAbnormal { get; set; }
        public bool? AdjustmentSplit { get; set; }
        public bool? AdjustmentFollowDPDF { get; set; }
        public bool? CalendarCodeOverride { get; set; }
        public IList<KeyValuePair<string, string>> Overrides { get; set; }
        public override bool MapTickers { get { return true; } }

        public override IEnumerable<Request> CreateRequests(Service refDataService)
        {
            Request request = refDataService.CreateRequest("HistoricalDataRequest");

            foreach (string ticker in Tickers)
                request.Append("securities", ticker);

            foreach (string field in Fields)
                request.Append("fields", field);

            request.Set("startDate", StartDate.ToString("yyyyMMdd"));
            request.Set("endDate", EndDate.ToString("yyyyMMdd"));
            request.Set("periodicitySelection", PeriodicitySelection.ToString());
            request.Set("periodicityAdjustment", PeriodicityAdjustment.ToString());
            if (!string.IsNullOrEmpty(Currency))
                request.Set("currency", Currency);
            if (OverrideOption.HasValue)
                request.Set("overrideOption", OverrideOption.Value.ToString());
            if (PricingOption.HasValue)
                request.Set("pricingOption", PricingOption.Value.ToString());
            if (NonTradingDayFillOption.HasValue)
                request.Set("nonTradingDayFillOption", NonTradingDayFillOption.Value.ToString());
            if (NonTradingDayFillMethod.HasValue)
                request.Set("nonTradingDayFillMethod", NonTradingDayFillMethod.Value.ToString());
            if (MaxDataPoints.HasValue)
                request.Set("maxDataPoints", MaxDataPoints.Value);
            if (ReturnEids.HasValue)
                request.Set("returnEids", ReturnEids.Value);
            if (ReturnRelativeDate.HasValue)
                request.Set("returnRelativeDate", ReturnRelativeDate.Value);
            if (AdjustmentNormal.HasValue)
                request.Set("adjustmentNormal", AdjustmentNormal.Value);
            if (AdjustmentAbnormal.HasValue)
                request.Set("adjustmentAbnormal", AdjustmentAbnormal.Value);
            if (AdjustmentSplit.HasValue)
                request.Set("adjustmentSplit", AdjustmentSplit.Value);
            if (AdjustmentFollowDPDF.HasValue)
                request.Set("adjustmentFollowDPDF", AdjustmentFollowDPDF.Value);
            if (CalendarCodeOverride.HasValue)
                request.Set("calendarCodeOverride", CalendarCodeOverride.Value);

            if (Overrides != null)
            {
                foreach (var pair in Overrides)
                {
                    Element requestOverride = request["overrides"].AppendElement();
                    requestOverride.SetElement("fieldId", pair.Key);
                    requestOverride.SetElement("value", pair.Value);
                }
            }

            return new[] { request };
        }
    }
}
