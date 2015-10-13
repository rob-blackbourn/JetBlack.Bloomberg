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
        private readonly Session _session;
        private Service _apiAuthService;
        private UserHandle _userHandle;
        private AuthenticationState _authenticationState = AuthenticationState.Pending;
        private readonly EventWaitHandle _autoResetEvent = new AutoResetEvent(false);
        private readonly string _clientHostname;
        private readonly string _uuid;

        public Authenticator(Session session, BloombergWrapper wrapper, string clientHostname, string uuid)
        {
            _uuid = uuid;
            _clientHostname = clientHostname;
            _session = session;
            wrapper.OnAuthenticationStatus += OnAuthenticationStatus;
        }

        private void OnAuthenticationStatus(bool isSuccess)
        {
            _authenticationState = isSuccess ? AuthenticationState.Succeeded : AuthenticationState.Failed;
            _autoResetEvent.Set();
        }

        public bool Authorise()
        {
            if (!_session.OpenService("//blp/apiauth"))
                throw new Exception("Failed to open service \"//blp/apiAuth\"");
            _apiAuthService = _session.GetService("//blp/apiauth");

            var authorizationRequest = _apiAuthService.CreateAuthorizationRequest();
            string clientIpAddress;

            try
            {
                var ipEntry = Dns.GetHostEntry(_clientHostname ?? String.Empty);
                var ipAddresses = ipEntry.AddressList;
                var ipAddress = ipAddresses.First(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                clientIpAddress = ipAddress.ToString();
            }
            catch
            {
                throw new ApplicationException(string.Format("Could not find ipaddress for hostname {0}", _clientHostname));
            }

            authorizationRequest.Set("uuid", _uuid);
            authorizationRequest.Set("ipAddress", clientIpAddress);

            lock (_session)
            {
                _userHandle = _session.CreateUserHandle();
                var correlationId = new CorrelationID(-1000);
                _session.SendAuthorizationRequest(authorizationRequest, _userHandle, correlationId);
            }

            _autoResetEvent.WaitOne();
            return _authenticationState == AuthenticationState.Succeeded;
        }

        public bool Permits(Element eidData, Service service)
        {
            if (_authenticationState != AuthenticationState.Succeeded) return false;
            if (eidData == null) return true;
            var missingEntitlements = new List<int>();
            return _userHandle.HasEntitlements(eidData, service, missingEntitlements);
        }

        public AuthenticationState AuthenticationState
        {
            get { return _authenticationState; }
        }
    }
}
