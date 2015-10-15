using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg
{
    public static class MessageTypeNames
    {
        public static readonly Name AuthorizationFailure = new Name("AuthorizationFailure");
        public static readonly Name AuthorizationSuccess = new Name("AuthorizationSuccess");
        public static readonly Name IntradayBarResponse = new Name("IntradayBarResponse");
        public static readonly Name IntradayTickResponse = new Name("IntradayTickResponse");
        public static readonly Name HistoricalDataResponse = new Name("HistoricalDataResponse");
        public static readonly Name ReferenceDataResponse = new Name("ReferenceDataResponse");
        public static readonly Name SubscriptionFailure = Name.GetName("SubscriptionFailure");
        public static readonly Name SubscriptionStarted = Name.GetName("SubscriptionStarted");
        public static readonly Name SubscriptionTerminated = Name.GetName("SubscriptionTerminated");
        public static readonly Name ServiceOpened = Name.GetName("ServiceOpened");
        public static readonly Name ServiceOpenFailure = Name.GetName("ServiceOpenFailure");
        public static readonly Name SessionConnectionDown = Name.GetName("SessionConnectionDown");
        public static readonly Name SessionConnectionUp = Name.GetName("SessionConnectionUp");
        public static readonly Name SessionStarted = Name.GetName("SessionStarted");
        public static readonly Name SessionStartupFailure = Name.GetName("SessionStartupFailure");
        public static readonly Name SessionTerminated = Name.GetName("SessionTerminated");
        public static readonly Name TokenGenerationFailure = Name.GetName("TokenGenerationFailure");
        public static readonly Name TokenGenerationSuccess = Name.GetName("TokenGenerationSuccess");
    }
}