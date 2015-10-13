using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg
{
    internal interface IAuthenticator
    {
        bool Authorise();
        bool Permits(Element eidData, Service service);
        AuthenticationState AuthenticationState { get; }
    }
}
