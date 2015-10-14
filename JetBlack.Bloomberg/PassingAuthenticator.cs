using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg
{
    internal class PassingAuthenticator : IAuthenticator
    {
        public bool Authenticate(Session session, string clientHostname, string uuid)
        {
            return true;
        }

        public bool Permits(Element eidData, Service service)
        {
            return true;
        }

        public AuthenticationState AuthenticationState
        {
            get { return AuthenticationState.Succeeded; }
        }
    }
}
