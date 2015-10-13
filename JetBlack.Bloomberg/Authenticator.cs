using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace JetBlack.Bloomberg
{
    internal class Authenticator : IAuthenticator
    {
        private Session session;
        private Service apiAuthService;
        private UserHandle userHandle;
        private AuthenticationState authenticationState = AuthenticationState.Pending;
        private EventWaitHandle autoResetEvent = new AutoResetEvent(false);
        private string clientHostname;
        private string uuid;

        public Authenticator(Session session, BloombergWrapper wrapper, string clientHostname, string uuid)
        {
            this.uuid = uuid;
            this.clientHostname = clientHostname;
            this.session = session;
            wrapper.OnAuthenticationStatus += wrapper_OnAuthenticationStatus;
        }

        private void wrapper_OnAuthenticationStatus(bool isSuccess)
        {
            authenticationState = isSuccess ? AuthenticationState.Succeeded : AuthenticationState.Failed;
            autoResetEvent.Set();
        }

        public bool Authorise()
        {
            if (!session.OpenService("//blp/apiauth"))
                throw new Exception("Failed to open service \"//blp/apiAuth\"");
            apiAuthService = session.GetService("//blp/apiauth");

            Request authorizationRequest = apiAuthService.CreateAuthorizationRequest();
            string clientIpAddress;

            try
            {
                IPHostEntry ipEntry = Dns.GetHostEntry(clientHostname ?? String.Empty);
                IPAddress[] ipAddresses = ipEntry.AddressList;
                IPAddress ipAddress = ipAddresses.First(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                clientIpAddress = ipAddress.ToString();
            }
            catch
            {
                throw new ApplicationException(string.Format("Could not find ipaddress for hostname {0}", clientHostname));
            }

            authorizationRequest.Set("uuid", uuid);
            authorizationRequest.Set("ipAddress", clientIpAddress);

            lock (session)
            {
                userHandle = session.CreateUserHandle();
                CorrelationID correlationId = new CorrelationID(-1000);
                session.SendAuthorizationRequest(authorizationRequest, userHandle, correlationId);
            }

            autoResetEvent.WaitOne();
            return authenticationState == AuthenticationState.Succeeded;
        }

        public bool Permits(Element eidData, Service service)
        {
            if (authenticationState != AuthenticationState.Succeeded) return false;
            if (eidData == null) return true;
            List<int> missingEntitlements = new List<int>();
            return userHandle.HasEntitlements(eidData, service, missingEntitlements);
        }

        public AuthenticationState AuthenticationState
        {
            get { return authenticationState; }
        }
    }
}
