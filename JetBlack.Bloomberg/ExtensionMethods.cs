using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JetBlack.Bloomberg
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
            else if (field.IsComplexType)
            {
                if (field.IsArray)
                {
                    Dictionary<string, object> values = new Dictionary<string, object>();
                    foreach (Element element in field.Elements)
                        values.Add(element.Name.ToString(), GetFieldValue(element));
                    return values;
                }
                else
                {
                    if (field.NumElements == 1)
                    {
                        Element element = field.GetElement(0);
                        // Do we need to switch on datatype?
                        object value = element.GetValue();
                        return value;
                    }
                    else
                    {
                        Dictionary<string, object> data = new Dictionary<string, object>();
                        foreach (Element element in field.Elements)
                            data.Add(element.Name.ToString(), GetFieldValue(element));
                        return data;
                    }
                }
            }
            else if (field.IsArray)
            {
                object[] values = new object[field.NumValues];
                for (int i = 0; i < field.NumValues; ++i)
                    values[i] = GetFieldValue(field.GetValueAsElement(i));
                return values;
            }
            else
            {
                object value = field.GetValue();
                switch (field.Datatype)
                {
                    case Schema.Datatype.DATE:
                        {
                            Datetime d = field.GetValueAsDate();
                            if (d.Year == 0)
                                return default(Datetime);

                            return new DateTime(d.Year, d.Month, d.DayOfMonth);
                        }
                    case Schema.Datatype.DATETIME:
                        {
                            Datetime d = field.GetValueAsDatetime();
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
                            Datetime d = field.GetValueAsTime();
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
        }

    }
}
