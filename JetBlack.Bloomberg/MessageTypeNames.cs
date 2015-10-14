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
        public static readonly Name TokenGenerationFailure = Name.GetName("TokenGenerationFailure");
        public static readonly Name TokenGenerationSuccess = Name.GetName("TokenGenerationSuccess");
    }
}