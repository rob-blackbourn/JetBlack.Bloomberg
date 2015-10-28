using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Responses;

namespace JetBlack.Bloomberg.Requests
{
    public class IntradayTickRequest
    {
        public string Ticker { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public IEnumerable<EventType> EventTypes { get; set; }
        public bool? IncludeConditionCodes { get; set; }
        public bool? IncludeNonPlottableEvents { get; set; }
        public bool? IncludeExchangeCodes { get; set; }
        public bool? ReturnEids { get; set; }
        public bool? IncludeBrokerCodes { get; set; }
        public bool? IncludeRpsCodes { get; set; }

        public static IntradayTickRequest Create(string ticker, IEnumerable<EventType> eventTypes, DateTime startDateTime, DateTime endDateTime)
        {
            return new IntradayTickRequest
            {
                Ticker = ticker,
                EventTypes = eventTypes,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime
            };
        }

        internal Request ToRequest(Service refDataService)
        {
            var request = refDataService.CreateRequest(OperationNames.IntradayTickRequest);
            request.Set(ElementNames.Security, Ticker);

            foreach (var eventType in EventTypes)
                request.Append(ElementNames.EventTypes, eventType.ToString());

            request.Set(ElementNames.StartDateTime, new Datetime(StartDateTime.Year, StartDateTime.Month, StartDateTime.Day, StartDateTime.Hour, StartDateTime.Minute, StartDateTime.Second, StartDateTime.Millisecond));
            request.Set(ElementNames.EndDateTime, new Datetime(EndDateTime.Year, EndDateTime.Month, EndDateTime.Day, EndDateTime.Hour, EndDateTime.Minute, EndDateTime.Second, EndDateTime.Millisecond));
            if (IncludeBrokerCodes.HasValue)
                request.Set(ElementNames.IncludeBrokerCodes, IncludeBrokerCodes.Value);
            if (IncludeConditionCodes.HasValue)
                request.Set(ElementNames.IncludeConditionCodes, IncludeConditionCodes.Value);
            if (IncludeExchangeCodes.HasValue)
                request.Set(ElementNames.IncludeExchangeCodes, IncludeExchangeCodes.Value);
            if (IncludeNonPlottableEvents.HasValue)
                request.Set(ElementNames.IncludeNonPlottableEvents, IncludeNonPlottableEvents.Value);
            if (IncludeRpsCodes.HasValue)
                request.Set(ElementNames.IncludeRpsCodes, IncludeRpsCodes.Value);
            if (ReturnEids.HasValue)
                request.Set(ElementNames.ReturnEids, ReturnEids.Value);
            return request;
        }
    }
}
