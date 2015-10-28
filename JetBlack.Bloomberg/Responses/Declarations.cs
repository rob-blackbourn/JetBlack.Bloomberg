namespace JetBlack.Bloomberg.Responses
{
    public enum SessionStatus
    {
        Started,
        Terminated,
        StartupFailure,
        ConnectionUp,
        ConnectionDown
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
}