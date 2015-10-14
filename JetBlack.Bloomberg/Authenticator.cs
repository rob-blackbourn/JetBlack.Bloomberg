using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using JetBlack.Bloomberg.Messages;

namespace JetBlack.Bloomberg
{
    internal class Authenticator : IAuthenticator
    {
        private Service _apiAuthService;
        private Identity _identity;
        private AuthenticationState _authenticationState = AuthenticationState.Pending;
        private readonly EventWaitHandle _autoResetEvent = new AutoResetEvent(false);

        public Authenticator(BloombergWrapper wrapper)
        {
            wrapper.OnAuthenticationStatus += OnAuthenticationStatus;
        }

        private void OnAuthenticationStatus(object sender, AuthenticationStatusEventArgs args)
        {
            _authenticationState = args.IsSuccess ? AuthenticationState.Succeeded : AuthenticationState.Failed;
            _autoResetEvent.Set();
        }

        public bool Authenticate(Session session, string clientHostname, string uuid)
        {
            if (!session.OpenService("//blp/apiauth"))
                throw new Exception("Failed to open service \"//blp/apiAuth\"");
            _apiAuthService = session.GetService("//blp/apiauth");

            var authorizationRequest = _apiAuthService.CreateAuthorizationRequest();
            string clientIpAddress;

            try
            {
                var ipEntry = Dns.GetHostEntry(clientHostname ?? String.Empty);
                var ipAddresses = ipEntry.AddressList;
                var ipAddress = ipAddresses.First(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                clientIpAddress = ipAddress.ToString();
            }
            catch
            {
                throw new ApplicationException(string.Format("Could not find ipaddress for hostname {0}", clientHostname));
            }

            authorizationRequest.Set("uuid", uuid);
            authorizationRequest.Set("ipAddress", clientIpAddress);

            _identity = session.CreateIdentity();
            var correlationId = new CorrelationID(-1000);
            session.SendAuthorizationRequest(authorizationRequest, _identity, correlationId);

            _autoResetEvent.WaitOne();
            return _authenticationState == AuthenticationState.Succeeded;
        }

        public bool Permits(Element eidData, Service service)
        {
            if (_authenticationState != AuthenticationState.Succeeded) return false;
            if (eidData == null) return true;
            var missingEntitlements = new List<int>();
            return _identity.HasEntitlements(eidData, service, missingEntitlements);
        }

        public AuthenticationState AuthenticationState
        {
            get { return _authenticationState; }
        }
    }
}
