using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg
{
    internal interface IAuthenticator
    {
        bool Authenticate(Session session, string clientHostname, string uuid);
        bool Permits(Element eidData, Service service);
        AuthenticationState AuthenticationState { get; }
    }
}
