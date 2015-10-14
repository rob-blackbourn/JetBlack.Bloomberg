using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Requesters
{
    public class IntradayBarRequester : Requester
    {
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public EventType EventType { get; set; }
        public int? Interval { get; set; }
        public bool? GapFillInitialBar { get; set; }
        public bool? ReturnEids { get; set; }
        public bool? ReturnRelativeDate { get; set; }
        public bool? AdjustmentNormal { get; set; }
        public bool? AdjustmentAbnormal { get; set; }
        public bool? AdjustmentSplit { get; set; }
        public bool? AdjustmentFollowDpdf { get; set; }
        public override bool MapTickers { get { return true; } }

        public override IEnumerable<Request> CreateRequests(Service refDataService)
        {
            var requests = new List<Request>();

            foreach (var ticker in Tickers)
            {
                var request = refDataService.CreateRequest("IntradayBarRequest");

                request.Set("security", ticker);
                request.Set("startDateTime", new Datetime(StartDateTime.Year, StartDateTime.Month, StartDateTime.Day, StartDateTime.Hour, StartDateTime.Minute, StartDateTime.Second, StartDateTime.Millisecond));
                request.Set("endDateTime", new Datetime(EndDateTime.Year, EndDateTime.Month, EndDateTime.Day, EndDateTime.Hour, EndDateTime.Minute, EndDateTime.Second, EndDateTime.Millisecond));
                request.Set("eventType", EventType.ToString());
                if (Interval.HasValue)
                    request.Set("interval", Interval.Value);
                if (GapFillInitialBar.HasValue)
                    request.Set("gapFillInitialBar", GapFillInitialBar.Value);
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
                if (AdjustmentFollowDpdf.HasValue)
                    request.Set("adjustmentFollowDPDF", AdjustmentFollowDpdf.Value);

                requests.Add(request);
            }

            return requests;
        }
    }
}
