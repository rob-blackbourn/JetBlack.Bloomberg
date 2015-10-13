using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg
{
    public enum SessionStatus
    {
        Started,
        Stopped
    }

    public enum ServiceStatus
    {
        Opened,
        Closed
    }

    public enum AdminStatus
    {
        SlowConsumerWarning,
        SlowConsumerWarningCleared
    }

    public enum SubscriptionStatus
    {
        None,
        Failure,
        Terminated
    }

    public enum FieldSubscriptionStatus
    {
        None,
        Started
    }

    public enum ResponseStatus
    {
        None,
        InvalidSecurity
    }

    public enum PeriodicitySelection
    {
        DAILY,
        WEEKLY,
        MONTHLY,
        QUARTERLY,
        SEMI_ANNUALLY,
        YEARLY
    }

    public enum PeriodicityAdjustment
    {
        ACTUAL,
        CALENDAR,
        FISCAL
    }

    public enum OverrideOption
    {
        OVERRIDE_OPTION_CLOSE,
        OVERRIDE_OPTION_GPA
    }

    public enum PricingOption
    {
        PRICING_OPTION_PRICE,
        PRICING_OPTION_YIELD
    }

    public enum NonTradingDayFillOption
    {
        NON_TRADING_WEEKDAYS,
        ALL_CALENDAR_DAYS,
        ACTIVE_DAYS_ONLY
    }

    public enum NonTradingDayFillMethod
    {
        PREVIOUS_VALUE,
        NIL_VALUE
    }

    public enum EventType
    {
        TRADE,
        BID,
        ASK,
        BID_BEST,
        ASK_BEST,
        MID_PRICE,
        AT_TRADE,
        BEST_BID,
        BEST_ASK
    }

    internal enum AuthenticationState
    {
        Succeeded,
        Failed,
        Pending
    }

    public delegate void NotifyErrorResponseEventHandler(string name, string messageType, string responseError);
    public delegate void IntradayBarReceivedEventHandler(string name, IList<IntradayBar> intradayTickDataList, IEnumerable<int> eids);
    public delegate void IntradayTickDataReceivedEventHandler(string name, IList<IntradayTickData> intradayBarList, IList<int> entitlementIds);
    public delegate void HistoricalDataReceivedEventHandler(string name, IDictionary<DateTime, IDictionary<string, object>> historicalDataMessage);
    public delegate void DataReceivedEventHandler(string name, IDictionary<string, object> referenceDataMessage);
    public delegate void SessionStatusEventHandler(SessionStatus sessionStatus);
    public delegate void ServiceStatusEventHandler(string name, ServiceStatus serviceStatus);
    public delegate void AdminStatusEventHandler(AdminStatus adminStatus);
    public delegate void ResponseStatusEventHandler(string name, ResponseStatus responseStatus, string source, string category, int code, string subCategory, string message);
    public delegate void SubscriptionStatusEventHandler(string name, SubscriptionStatus subscriptionStatus, string source, string category, int errorCode, string description);
    public delegate void FieldSubscriptionStatusEventHandler(string name, string fieldId, FieldSubscriptionStatus fieldSubscriptionStatus, string source, string category, string subCategory, int errorCode, string description);
    public delegate void AuthenticationEventHandler(bool isSuccess);
    public delegate void AuthenticationErrorEventHandler(string ticker, string messageType, IEnumerable<int> eids);
}