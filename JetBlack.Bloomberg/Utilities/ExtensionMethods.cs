using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Models;

namespace JetBlack.Bloomberg.Utilities
{
    internal static class ExtensionMethods
    {
        public static DateTime ToDateTime(this Datetime datetime)
        {
            return new DateTime(datetime.Year, datetime.Month, datetime.DayOfMonth, datetime.Hour, datetime.Minute, datetime.Second, datetime.MilliSecond);
        }

        public static DateTime ToDate(this Datetime datetime)
        {
            return new DateTime(datetime.Year, datetime.Month, datetime.DayOfMonth, 0, 0, 0, 0);
        }

        public static Datetime ToDateTime(this DateTime datetime)
        {
            return new Datetime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond);
        }

        public static Datetime ToDate(this DateTime datetime)
        {
            return new Datetime(datetime.Year, datetime.Month, datetime.Day, 0, 0, 0, 0);
        }

        public static object GetFieldValue(this Element field)
        {
            if (field.IsNull)
                return null;

            if (field.IsComplexType)
            {
                if (field.IsArray)
                {
                    return field.Elements.ToDictionary(element => element.Name.ToString(), GetFieldValue);
                }

                if (field.NumElements == 1)
                {
                    var element = field.GetElement(0);
                    // Do we need to switch on datatype?
                    return element.GetValue();
                }

                return field.Elements.ToDictionary(element => element.Name.ToString(), GetFieldValue);
            }

            if (field.IsArray)
            {
                var values = new object[field.NumValues];
                for (var i = 0; i < field.NumValues; ++i)
                    values[i] = GetFieldValue(field.GetValueAsElement(i));
                return values;
            }

            switch (field.Datatype)
            {
                case Schema.Datatype.DATE:
                {
                    var d = field.GetValueAsDate();
                    if (d.Year == 0)
                        return default(DateTime);

                    return new DateTime(d.Year, d.Month, d.DayOfMonth);
                }
                case Schema.Datatype.DATETIME:
                {
                    var d = field.GetValueAsDatetime();
                    if (d.Year == 0)
                        return default(Datetime);

                    return new DateTime(d.Year, d.Month, d.DayOfMonth, d.Hour, d.Minute, d.Second, d.MilliSecond, DateTimeKind.Utc);
                }
                case Schema.Datatype.FLOAT32:
                    return Convert.ToDouble(field.GetValue());

                case Schema.Datatype.SEQUENCE:
                    Trace.TraceInformation("SEQUENCE: {0}", field.GetValue());
                    return field.GetValue();

                case Schema.Datatype.ENUMERATION:
                    Trace.TraceInformation("ENUMERATION: {0}", field.GetValue());
                    return field.GetValue();

                case Schema.Datatype.CHOICE:
                    Trace.TraceInformation("CHOICE: {0}", field.GetValue());
                    return field.GetValue();

                case Schema.Datatype.TIME:
                {
                    var d = field.GetValueAsTime();
                    if (d.Year != 1)
                        Trace.TraceWarning("Oh Dear");

                    return new TimeSpan(0, d.Hour, d.Minute, d.Second, d.MilliSecond);
                }

                case Schema.Datatype.FLOAT64:
                    return field.GetValueAsFloat64();

                default:
                    return field.GetValue();
            }
        }

        public static IList<int> ExtractEids(this Element eidDataElement)
        {
            var eids = new List<int>();

            if (eidDataElement != null)
            {
                for (var i = 0; i < eidDataElement.NumValues; ++i)
                {
                    var eid = eidDataElement.GetValueAsInt32(i);
                    eids.Add(eid);
                }
            }

            return eids;
        }

        public static TokenGenerationFailure ToTokenGenerationFailureEventArgs(this Element reason)
        {
            return
                new TokenGenerationFailure(
                    reason.GetElementAsString(ElementNames.Source),
                    reason.GetElementAsInt32(ElementNames.ErrorCode),
                    reason.GetElementAsString(ElementNames.Category),
                    reason.GetElementAsString(ElementNames.SubCategory),
                    reason.GetElementAsString(ElementNames.Description));
        }

        public static SubscriptionFailureEventArgs ToSubscriptionFailureEventArgs(this Message message)
        {
            var reason = message.GetElement(ElementNames.Reason);
            return new SubscriptionFailureEventArgs(message.TopicName,
                reason.GetElement(ElementNames.Source).GetValueAsString(),
                reason.GetElement(ElementNames.Category).GetValueAsString(),
                reason.GetElement(ElementNames.ErrorCode).GetValueAsInt32(),
                reason.GetElement(ElementNames.Description).GetValueAsString());
        }

        public static SecurityError ToSecurityError(this Element securityError)
        {
            return
                new SecurityError(
                    securityError.GetElementAsString(ElementNames.Source), 
                    securityError.GetElementAsString(ElementNames.Category),
                    securityError.GetElementAsString(ElementNames.SubCategory),
                    securityError.GetElementAsInt32(ElementNames.Code),
                    securityError.GetElementAsString(ElementNames.Message));
        }

        public static ResponseError ToResponseError(this Element response)
        {
            return
                new ResponseError(
                    response.GetElementAsString(ElementNames.Source),
                    response.GetElementAsString(ElementNames.Category),
                    response.GetElementAsString(ElementNames.SubCategory),
                    response.GetElementAsInt32(ElementNames.Code),
                    response.GetElementAsString(ElementNames.Message));
        }
    }

    public static class KeyValuePair
    {
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
                action(item);
        }
    }
}
