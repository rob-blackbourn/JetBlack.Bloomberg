using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Authenticators;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Managers;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Utilities;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public class BloombergController : ITokenProvider, ISecurityEntitlementsProvider, IReferenceDataProvider, IHistoricalDataProvider, IIntradayBarProvider, IIntradayTickProvider, ISubscriptionProvider
    {
        public event EventHandler<EventArgs<SessionStatus>> SessionStatus;
        public event EventHandler<EventArgs<AdminStatus>> AdminStatus;
        public event EventHandler<EventArgs<bool>> AuthenticationStatus;
        public event EventHandler<EventArgs<bool>> InitialisationStatus;

        private readonly Func<BloombergController, IAuthenticator> _authenticatorFactory;
        private readonly Session _session;
        private readonly TokenManager _tokenManager;
        private readonly ServiceManager _serviceManager;

        private Identity _identity;
        private Service _authorisationService;
        private Service _marketDataService;
        private Service _referenceDataService;
        private SecurityEntitlementsManager _securityEntitlementsManager;
        private ReferenceDataManager _referenceDataManager;
        private HistoricalDataManager _historicalDataManager;
        private IntradayBarManager _intradayBarManager;
        private IntradayTickManager _intradayTickManager;
        private SubscriptionManager _subscriptionManager;
        private IAuthenticator _authenticator;

        public BloombergController(SessionOptions sessionOptions, Func<BloombergController, IAuthenticator> authenticatorFactory)
        {
            _authenticatorFactory = authenticatorFactory;
            _session = new Session(sessionOptions, HandleMessage);

            _tokenManager = new TokenManager(_session);
            _serviceManager = new ServiceManager(_session);
        }

        public void Start()
        {
            if (!_session.Start())
                throw new Exception("Failed to start session");

            _identity = _session.CreateIdentity();

            _authorisationService = OpenService(ServiceUris.AuthenticationService);
            _securityEntitlementsManager = new SecurityEntitlementsManager(_session, _authorisationService, _identity);

            _authenticator = _authenticatorFactory(this);
            _authenticator.Authenticate(_session, _authorisationService, _identity);

            _marketDataService = OpenService(ServiceUris.MarketDataService);
            _subscriptionManager = new SubscriptionManager(_session, _identity);

            _referenceDataService = OpenService(ServiceUris.ReferenceDataService);
            _referenceDataManager = new ReferenceDataManager(_session, _referenceDataService, _identity);
            _historicalDataManager = new HistoricalDataManager(_session, _referenceDataService, _identity);
            _intradayBarManager = new IntradayBarManager(_session, _referenceDataService, _identity);
            _intradayTickManager = new IntradayTickManager(_session, _referenceDataService, _identity);

            RaiseEvent(InitialisationStatus, new EventArgs<bool>(true));
        }

        public void StartAsync()
        {
            _session.StartAsync();
        }

        public void AuthenticateAsync()
        {
            _identity = _session.CreateIdentity();
            _authenticator = _authenticatorFactory(this);

            _serviceManager.Request(ServiceUris.AuthenticationService)
                .Then(service =>
                {
                    _authorisationService = service;
                    _securityEntitlementsManager = new SecurityEntitlementsManager(_session, _authorisationService, _identity);
                    return _authenticator.Request(_session, service, _identity);
                })
                .Then(isAuthenticated =>
                {
                    RaiseEvent(AuthenticationStatus, new EventArgs<bool>(isAuthenticated));
                    return isAuthenticated ? Promise.Resolved() : Promise.Rejected(new ApplicationException("Authentication failed"));
                })
                .ThenAll(() => new[]
                {
                    _serviceManager.Request(ServiceUris.ReferenceDataService)
                        .Then(service =>
                        {
                            _referenceDataService = service;
                            _referenceDataManager = new ReferenceDataManager(_session, _referenceDataService, _identity);
                            _historicalDataManager = new HistoricalDataManager(_session, _referenceDataService, _identity);
                            _intradayBarManager = new IntradayBarManager(_session, _referenceDataService, _identity);
                            _intradayTickManager = new IntradayTickManager(_session, _referenceDataService, _identity);
                            return Promise.Resolved();
                        }),
                    _serviceManager.Request(ServiceUris.MarketDataService)
                        .Then(service =>
                        {
                            _marketDataService = service;
                            _subscriptionManager = new SubscriptionManager(_session, _identity);
                            return Promise.Resolved();
                        })
                }).Done(
                    () => RaiseEvent(InitialisationStatus, new EventArgs<bool>(true)),
                    _ => RaiseEvent(InitialisationStatus, new EventArgs<bool>(false)));
        }

        public void Stop()
        {
            _session.Stop(AbstractSession.StopOption.SYNC);
        }

        public void StopAsync()
        {
            _session.Stop(AbstractSession.StopOption.ASYNC);
        }

        public string GenerateToken()
        {
            return _tokenManager.GenerateToken();
        }

        public IPromise<string> RequestToken()
        {
            return _tokenManager.RequestToken();
        }

        public Service OpenService(string uri)
        {
            return _serviceManager.Open(uri);
        }

        public IPromise<Service> RequestService(string uri)
        {
            return _serviceManager.Request(uri);
        }

        public IPromise<ICollection<SecurityEntitlements>> RequestSecurityEntitlements(IEnumerable<string> tickers)
        {
            return _securityEntitlementsManager.RequestSecurityEntitlements(tickers);
        }

        public IObservable<TickerData> ToObservable(IEnumerable<string> tickers, IEnumerable<string> fields)
        {
            return _subscriptionManager.ToObservable(tickers, fields);
        }

        public IPromise<IntradayTickResponse> RequestIntradayTick(IntradayTickRequest request)
        {
            return _intradayTickManager.RequestIntradayTick(request);
        }

        public IPromise<ReferenceDataResponse> RequestReferenceData(ReferenceDataRequest request)
        {
            return _referenceDataManager.RequestReferenceData(request);
        }

        public IPromise<HistoricalDataResponse> RequestHistoricalData(HistoricalDataRequest request)
        {
            return _historicalDataManager.RequestHistoricalData(request);
        }

        public IPromise<IntradayBarResponse> RequestIntradayBar(IntradayBarRequest request)
        {
            return _intradayBarManager.RequestIntradayBar(request);
        }

        private void HandleMessage(Event eventArgs, Session session)
        {
            try
            {
                switch (eventArgs.Type)
                {
                    case Event.EventType.PARTIAL_RESPONSE:
                    case Event.EventType.RESPONSE:
                        ProcessResponse(session, eventArgs, eventArgs.Type == Event.EventType.PARTIAL_RESPONSE);
                        break;

                    case Event.EventType.SUBSCRIPTION_DATA:
                        eventArgs.ForEach(message => _subscriptionManager.ProcessSubscriptionData(session, message, false));
                        break;

                    case Event.EventType.SUBSCRIPTION_STATUS:
                        eventArgs.ForEach(message => _subscriptionManager.ProcessSubscriptionStatus(session, message));
                        break;

                    case Event.EventType.SESSION_STATUS:
                        eventArgs.ForEach(ProcessSessionStatus);
                        break;

                    case Event.EventType.SERVICE_STATUS:
                        eventArgs.GetMessages().ForEach(message => _serviceManager.Process(session, message, OnFailure));
                        break;

                    case Event.EventType.ADMIN:
                        ProcessAdminMessage(eventArgs);
                        break;

                    case Event.EventType.TOKEN_STATUS:
                        eventArgs.GetMessages().ForEach(message => _tokenManager.ProcessTokenStatusEvent(session, message, OnFailure));
                        break;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleMessage: {0}", ex.Message);
            }
        }

        private void OnFailure(Session session, Message message, Exception error)
        {
        }

        private void ProcessAdminMessage(Event e)
        {
            if (!e.IsValid) return;

            foreach (var message in e.GetMessages())
            {
                switch (message.MessageType.ToString())
                {
                    case "SlowConsumerWarning":
                        RaiseEvent(AdminStatus, new EventArgs<AdminStatus>(Models.AdminStatus.SlowConsumerWarning));
                        break;
                    case "SlowConsumerWarningCleared":
                        RaiseEvent(AdminStatus, new EventArgs<AdminStatus>(Models.AdminStatus.SlowConsumerWarningCleared));
                        break;
                }
            }
        }

        private void ProcessResponse(Session session, Event eventArgs, bool isPartialResponse)
        {
            if (!eventArgs.IsValid)
            {
                // TODO: What should we do here?
                return;
            }

            foreach (var message in eventArgs.GetMessages())
            {
                if (message.MessageType.Equals(MessageTypeNames.AuthorizationFailure) || message.MessageType.Equals(MessageTypeNames.AuthorizationSuccess))
                    _authenticator.Process(session, message, OnFailure);
                if (message.MessageType.Equals(MessageTypeNames.IntradayBarResponse))
                    _intradayBarManager.Process(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.IntradayTickResponse))
                    _intradayTickManager.Process(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.HistoricalDataResponse))
                    _historicalDataManager.Process(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.ReferenceDataResponse))
                    _referenceDataManager.Process(session, message, isPartialResponse, OnFailure);
                else if (MessageTypeNames.SecurityEntitlementsResponse.Equals(message.MessageType))
                    _securityEntitlementsManager.Process(session, message, isPartialResponse, OnFailure);
            }
        }

        private void RaiseEvent<T>(EventHandler<T> handler, T args) where T:EventArgs
        {
            if (handler != null)
                handler(this, args);
        }

        private void ProcessSessionStatus(Message message)
        {
            if (MessageTypeNames.SessionStarted.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Models.SessionStatus.Started));
            if (MessageTypeNames.SessionTerminated.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Models.SessionStatus.Terminated));
            if (MessageTypeNames.SessionStartupFailure.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Models.SessionStatus.StartupFailure));
            if (MessageTypeNames.SessionConnectionUp.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Models.SessionStatus.ConnectionUp));
            if (MessageTypeNames.SessionConnectionDown.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Models.SessionStatus.ConnectionDown));
        }
    }
}
