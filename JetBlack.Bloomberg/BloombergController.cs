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
    public class BloombergController : ITokenManager, ISecurityEntitlementsManager
    {
        private readonly Func<BloombergController, IAuthenticator> _authenticatorFactory;
        public event EventHandler<EventArgs<SessionStatus>> SessionStatus;
        public event EventHandler<EventArgs<AdminStatus>> AdminStatus;
        public event EventHandler<EventArgs<bool>> AuthenticationStatus;
        public event EventHandler<EventArgs<bool>> InitialisationStatus;

        public Session Session { get; private set; }
        public Identity Identity { get; private set; }
        public Service AuthorisationService { get; private set; }
        public Service MarketDataService { get; private set; }
        public Service ReferenceDataService { get; private set; }

        private readonly TokenManager _tokenManager;
        private readonly ServiceManager _serviceManager;

        private SecurityEntitlementsManager _securityEntitlementsManager;

        public SubscriptionManager SubscriptionManager { get; private set; }
        public ReferenceDataManager ReferenceDataManager { get; private set; }
        public HistoricalDataManager HistoricalDataManager { get; private set; }
        public IntradayBarManager IntradayBarManager { get; private set; }
        public IntradayTickManager IntradayTickManager { get; private set; }

        public IAuthenticator Authenticator { get; private set; }

        public BloombergController(SessionOptions sessionOptions, Func<BloombergController, IAuthenticator> authenticatorFactory)
        {
            _authenticatorFactory = authenticatorFactory;
            Session = new Session(sessionOptions, HandleMessage);

            _tokenManager = new TokenManager(Session);
            _serviceManager = new ServiceManager(Session);
            SubscriptionManager = new SubscriptionManager();
            ReferenceDataManager = new ReferenceDataManager();
            HistoricalDataManager = new HistoricalDataManager();
            IntradayBarManager = new IntradayBarManager();
            IntradayTickManager = new IntradayTickManager();
        }

        public void Start()
        {
            if (!Session.Start())
                throw new Exception("Failed to start session");

            Identity = Session.CreateIdentity();

            AuthorisationService = OpenService(ServiceUris.AuthenticationService);
            _securityEntitlementsManager = new SecurityEntitlementsManager(Session, AuthorisationService, Identity);

            Authenticator = _authenticatorFactory(this);
            Authenticator.Authenticate(Session, AuthorisationService, Identity);

            MarketDataService = OpenService(ServiceUris.MarketDataService);
            ReferenceDataService = OpenService(ServiceUris.ReferenceDataService);
            RaiseEvent(InitialisationStatus, new EventArgs<bool>(true));
        }

        public void StartAsync()
        {
            Session.StartAsync();
        }

        public void AuthenticateAsync()
        {
            Identity = Session.CreateIdentity();
            Authenticator = _authenticatorFactory(this);

            _serviceManager.Request(ServiceUris.AuthenticationService)
                .Then(service =>
                {
                    AuthorisationService = service;
                    _securityEntitlementsManager = new SecurityEntitlementsManager(Session, AuthorisationService, Identity);
                    return Authenticator.Request(Session, service, Identity);
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
                            ReferenceDataService = service;
                            return Promise.Resolved();
                        }),
                    _serviceManager.Request(ServiceUris.MarketDataService)
                        .Then(service =>
                        {
                            MarketDataService = service;
                            return Promise.Resolved();
                        })
                }).Done(() =>
                {
                    RaiseEvent(InitialisationStatus, new EventArgs<bool>(true));
                }, _ =>
                {
                    RaiseEvent(InitialisationStatus, new EventArgs<bool>(false));
                });
        }

        public void Stop()
        {
            Session.Stop(AbstractSession.StopOption.SYNC);
        }

        public void StopAsync()
        {
            Session.Stop(AbstractSession.StopOption.ASYNC);
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

        public IPromise<ICollection<SecurityEntitlements>> RequestEntitlements(IEnumerable<string> tickers)
        {
            return _securityEntitlementsManager.RequestEntitlements(tickers);
        }

        public IObservable<TickerData> ToObservable(IEnumerable<string> tickers, IList<string> fields)
        {
            return SubscriptionManager.ToObservable(Session, Identity, tickers, fields);
        }

        public IPromise<TickerIntradayTickData> RequestIntradayTick(string ticker, IEnumerable<EventType> eventTypes, DateTime startDateTime, DateTime endDateTime)
        {
            return RequestIntradayTick(
                new IntradayTickRequest
                {
                    Ticker = ticker,
                    EventTypes = eventTypes,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime
                });
        }

        public IPromise<TickerIntradayTickData> RequestIntradayTick(IntradayTickRequest request)
        {
            return IntradayTickManager.Request(Session, Identity, ReferenceDataService, request);
            
        }

        public IPromise<IDictionary<string,IDictionary<string,object>>> RequestReferenceData(ReferenceDataRequest request)
        {
            return ReferenceDataManager.Request(Session, Identity, ReferenceDataService, request);
        }

        public IPromise<IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>> RequestHistoricalData(ICollection<string> tickers, IList<string> fields, DateTime startDate, DateTime endDate, PeriodicitySelection periodicitySelection)
        {
            return RequestHistoricalData(
                new HistoricalDataRequest
                {
                    Tickers = tickers,
                    Fields = fields,
                    StartDate = startDate,
                    EndDate = endDate,
                    PeriodicitySelection = periodicitySelection,
                    PeriodicityAdjustment = PeriodicityAdjustment.ACTUAL,
                    NonTradingDayFillOption = NonTradingDayFillOption.ACTIVE_DAYS_ONLY,
                    NonTradingDayFillMethod = NonTradingDayFillMethod.NIL_VALUE,
                });
        }

        public IPromise<IDictionary<string,IDictionary<DateTime,IDictionary<string,object>>>> RequestHistoricalData(HistoricalDataRequest request)
        {
            return HistoricalDataManager.Request(Session, Identity, ReferenceDataService, request);
        }

        public IPromise<TickerIntradayBarData> RequestIntradayBar(string ticker, DateTime startDateTime, DateTime endDateTime, EventType eventType, int interval)
        {
            return RequestIntradayBar(
                new IntradayBarRequest
                {
                    Ticker = ticker,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    EventType = eventType,
                    Interval = interval
                });
        }

        public IPromise<TickerIntradayBarData> RequestIntradayBar(IntradayBarRequest request)
        {
            return IntradayBarManager.Request(Session, Identity, ReferenceDataService, request);
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
                        eventArgs.ForEach(message => SubscriptionManager.ProcessSubscriptionData(session, message, false));
                        break;

                    case Event.EventType.SUBSCRIPTION_STATUS:
                        eventArgs.ForEach(message => SubscriptionManager.ProcessSubscriptionStatus(session, message));
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
                    Authenticator.Process(session, message, OnFailure);
                if (message.MessageType.Equals(MessageTypeNames.IntradayBarResponse))
                    IntradayBarManager.Process(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.IntradayTickResponse))
                    IntradayTickManager.Process(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.HistoricalDataResponse))
                    HistoricalDataManager.Process(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.ReferenceDataResponse))
                    ReferenceDataManager.Process(session, message, isPartialResponse, OnFailure);
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
