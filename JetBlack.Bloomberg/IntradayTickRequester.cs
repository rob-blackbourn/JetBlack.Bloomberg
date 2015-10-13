using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg
{
    public class IntradayTickRequester : Requester
    {
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public IEnumerable<EventType> EventTypes { get; set; }
        public bool? IncludeConditionCodes { get; set; }
        public bool? IncludeNonPlottableEvents { get; set; }
        public bool? IncludeExchangeCodes { get; set; }
        public bool? ReturnEids { get; set; }
        public bool? IncludeBrokerCodes { get; set; }
        public bool? IncludeRpsCodes { get; set; }
        public override bool MapTickers { get { return true; } }

        public override IEnumerable<Request> CreateRequests(Service refDataService)
        {
            List<Request> requests = new List<Request>();

            foreach (string ticker in Tickers)
            {
                Request request = refDataService.CreateRequest("IntradayTickRequest");
                request.Set("security", ticker);

                foreach (var eventType in EventTypes)
                    request.Append("eventTypes", eventType.ToString());

                request.Set("startDateTime", new Datetime(StartDateTime.Year, StartDateTime.Month, StartDateTime.Day, StartDateTime.Hour, StartDateTime.Minute, StartDateTime.Second, StartDateTime.Millisecond));
                request.Set("endDateTime", new Datetime(EndDateTime.Year, EndDateTime.Month, EndDateTime.Day, EndDateTime.Hour, EndDateTime.Minute, EndDateTime.Second, EndDateTime.Millisecond));
                if (IncludeBrokerCodes.HasValue)
                    request.Set("includeBrokerCodes", IncludeBrokerCodes.Value);
                if (IncludeConditionCodes.HasValue)
                    request.Set("includeConditionCodes", IncludeConditionCodes.Value);
                if (IncludeExchangeCodes.HasValue)
                    request.Set("includeExchangeCodes", IncludeExchangeCodes.Value);
                if (IncludeNonPlottableEvents.HasValue)
                    request.Set("includeNonPlottableEvents", IncludeNonPlottableEvents.Value);
                if (IncludeRpsCodes.HasValue)
                    request.Set("includeRpsCodes", IncludeRpsCodes.Value);
                if (ReturnEids.HasValue)
                    request.Set("returnEids", ReturnEids.Value);
                requests.Add(request);
            }
            return requests;
        }
    }
}
