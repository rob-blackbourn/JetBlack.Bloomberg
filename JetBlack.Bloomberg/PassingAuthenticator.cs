using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg
{
    internal class PassingAuthenticator : IAuthenticator
    {
        public bool Authorise()
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
