using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Requesters
{
    public class HistoricalDataRequestFactory : RequestFactory
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
        public bool? AdjustmentFollowDpdf { get; set; }
        public bool? CalendarCodeOverride { get; set; }
        public IList<KeyValuePair<string, string>> Overrides { get; set; }

        public override IEnumerable<Request> CreateRequests(Service refDataService)
        {
            var request = refDataService.CreateRequest(OperationNames.HistoricalDataRequest);

            foreach (var ticker in Tickers)
                request.Append(ElementNames.Securities, ticker);

            foreach (var field in Fields)
                request.Append(ElementNames.Fields, field);

            request.Set(ElementNames.StartDate, StartDate.ToString("yyyyMMdd"));
            request.Set(ElementNames.EndDate, EndDate.ToString("yyyyMMdd"));
            request.Set(ElementNames.PeriodicitySelection, PeriodicitySelection.ToString());
            request.Set(ElementNames.PeriodicityAdjustment, PeriodicityAdjustment.ToString());
            if (!string.IsNullOrEmpty(Currency))
                request.Set(ElementNames.Currency, Currency);
            if (OverrideOption.HasValue)
                request.Set(ElementNames.OverrideOption, OverrideOption.Value.ToString());
            if (PricingOption.HasValue)
                request.Set(ElementNames.PricingOption, PricingOption.Value.ToString());
            if (NonTradingDayFillOption.HasValue)
                request.Set(ElementNames.NonTradingDayFillOption, NonTradingDayFillOption.Value.ToString());
            if (NonTradingDayFillMethod.HasValue)
                request.Set(ElementNames.NonTradingDayFillMethod, NonTradingDayFillMethod.Value.ToString());
            if (MaxDataPoints.HasValue)
                request.Set(ElementNames.MaxDataPoints, MaxDataPoints.Value);
            if (ReturnEids.HasValue)
                request.Set(ElementNames.ReturnEids, ReturnEids.Value);
            if (ReturnRelativeDate.HasValue)
                request.Set(ElementNames.ReturnRelativeDate, ReturnRelativeDate.Value);
            if (AdjustmentNormal.HasValue)
                request.Set(ElementNames.AdjustmentNormal, AdjustmentNormal.Value);
            if (AdjustmentAbnormal.HasValue)
                request.Set(ElementNames.AdjustmentAbnormal, AdjustmentAbnormal.Value);
            if (AdjustmentSplit.HasValue)
                request.Set(ElementNames.AdjustmentSplit, AdjustmentSplit.Value);
            if (AdjustmentFollowDpdf.HasValue)
                request.Set(ElementNames.AdjustmentFollowDPDF, AdjustmentFollowDpdf.Value);
            if (CalendarCodeOverride.HasValue)
                request.Set(ElementNames.CalendarCodeOverride, CalendarCodeOverride.Value);

            if (Overrides != null)
            {
                foreach (var pair in Overrides)
                {
                    var requestOverride = request[ElementNames.Overrides].AppendElement();
                    requestOverride.SetElement(ElementNames.FieldId, pair.Key);
                    requestOverride.SetElement(ElementNames.Value, pair.Value);
                }
            }

            return new[] { request };
        }
    }
}
