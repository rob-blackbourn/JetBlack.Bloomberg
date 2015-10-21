using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Identifiers
{
    internal static class MessageTypeNames
    {
        public static readonly Name AuthorizationFailure = Name.GetName("AuthorizationFailure");
        public static readonly Name AuthorizationSuccess = Name.GetName("AuthorizationSuccess");
        public static readonly Name IntradayBarResponse = Name.GetName("IntradayBarResponse");
        public static readonly Name IntradayTickResponse = Name.GetName("IntradayTickResponse");
        public static readonly Name HistoricalDataResponse = Name.GetName("HistoricalDataResponse");
        public static readonly Name ReferenceDataResponse = Name.GetName("ReferenceDataResponse");
        public static readonly Name SecurityEntitlementsResponse = Name.GetName("SecurityEntitlementsResponse");
        public static readonly Name ServiceOpened = Name.GetName("ServiceOpened");
        public static readonly Name ServiceOpenFailure = Name.GetName("ServiceOpenFailure");
        public static readonly Name SessionConnectionDown = Name.GetName("SessionConnectionDown");
        public static readonly Name SessionConnectionUp = Name.GetName("SessionConnectionUp");
        public static readonly Name SessionStarted = Name.GetName("SessionStarted");
        public static readonly Name SessionStartupFailure = Name.GetName("SessionStartupFailure");
        public static readonly Name SessionTerminated = Name.GetName("SessionTerminated");
        public static readonly Name SubscriptionFailure = Name.GetName("SubscriptionFailure");
        public static readonly Name SubscriptionStarted = Name.GetName("SubscriptionStarted");
        public static readonly Name SubscriptionTerminated = Name.GetName("SubscriptionTerminated");
        public static readonly Name TokenGenerationFailure = Name.GetName("TokenGenerationFailure");
        public static readonly Name TokenGenerationSuccess = Name.GetName("TokenGenerationSuccess");
    }
}