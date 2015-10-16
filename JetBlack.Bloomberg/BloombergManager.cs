using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Authenticators;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requesters;
using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public class BloombergManager
    {
        private readonly Func<BloombergManager, IAuthenticator> _authenticatorFactory;
        public event EventHandler<EventArgs<SessionStatus>> SessionStatus;
        public event EventHandler<EventArgs<AdminStatus>> AdminStatus;
        public event EventHandler<EventArgs<bool>> AuthenticationStatus;
        public event EventHandler<EventArgs<bool>> InitialisationStatus;

        public Session Session { get; private set; }
        public Identity Identity { get; private set; }
        public Service AuthorisationService { get; private set; }
        public Service MarketDataService { get; private set; }
        public Service ReferenceDataService { get; private set; }

        public TokenManager TokenManager { get; private set; }
        public ServiceManager ServiceManager { get; private set; }
        public SubscriptionManager SubscriptionManager { get; private set; }
        public ReferenceDataManager ReferenceDataManager { get; private set; }
        public HistoricalDataManager HistoricalDataManager { get; private set; }
        public IntradayBarManager IntradayBarManager { get; private set; }
        public IntraDayTickManager IntraDayTickManager { get; private set; }

        public IAuthenticator Authenticator { get; private set; }

        public BloombergManager(SessionOptions sessionOptions, Func<BloombergManager, IAuthenticator> authenticatorFactory)
        {
            _authenticatorFactory = authenticatorFactory;
            Session = new Session(sessionOptions, HandleMessage);

            TokenManager = new TokenManager();
            ServiceManager = new ServiceManager();
            SubscriptionManager = new SubscriptionManager();
            ReferenceDataManager = new ReferenceDataManager();
            HistoricalDataManager = new HistoricalDataManager();
            IntradayBarManager = new IntradayBarManager();
            IntraDayTickManager = new IntraDayTickManager();
        }

        public void Start()
        {
            if (!Session.Start())
                throw new Exception("Failed to start session");

            MarketDataService = OpenService("//blp/mktdata");
            ReferenceDataService = OpenService("//blp/refdata");
        }

        public void StartAsync()
        {
            Session.StartAsync();
        }

        public void AuthenticateAsync()
        {
            Identity = Session.CreateIdentity();
            Authenticator = _authenticatorFactory(this);

            ServiceManager.Request(Session, ServiceUris.AuthenticationService)
                .Then(service =>
                {
                    AuthorisationService = service;
                    return Authenticator.Request(Session, service);
                })
                .Then(isAuthenticated =>
                {
                    RaiseEvent(AuthenticationStatus, new EventArgs<bool>(isAuthenticated));
                    return isAuthenticated ? Promise.Resolved() : Promise.Rejected(new ApplicationException("Authentication failed"));
                })
                .ThenAll(() => new[]
                {
                    ServiceManager.Request(Session, ServiceUris.ReferenceDataService)
                        .Then(service =>
                        {
                            ReferenceDataService = service;
                            return Promise.Resolved();
                        }),
                    ServiceManager.Request(Session, ServiceUris.MarketDataService)
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
            return TokenManager.GenerateToken(Session);
        }

        public IPromise<string> RequestToken()
        {
            return TokenManager.Request(Session);
        }

        public Service OpenService(string uri)
        {
            return ServiceManager.Open(Session, uri);
        }

        public IPromise<Service> RequestService(string uri)
        {
            return ServiceManager.Request(Session, uri);
        }

        public IObservable<TickerData> ToObservable(IEnumerable<string> tickers, IList<string> fields)
        {
            return SubscriptionManager.ToObservable(Session, Identity, tickers, fields);
        }

        public IPromise<TickerIntradayTickData> RequestIntradayTick(ICollection<string> tickers, IEnumerable<EventType> eventTypes, DateTime startDateTime, DateTime endDateTime)
        {
            return RequestIntradayTick(
                new IntradayTickRequestFactory
                {
                    Tickers = tickers,
                    EventTypes = eventTypes,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime
                });
        }

        public IPromise<TickerIntradayTickData> RequestIntradayTick(IntradayTickRequestFactory request)
        {
            return IntraDayTickManager.Request(Session, Identity, ReferenceDataService, request);
            
        }

        public IPromise<IDictionary<string,IDictionary<string,object>>> RequestReferenceData(ReferenceDataRequestFactory requestFactory)
        {
            return ReferenceDataManager.Request(Session, Identity, ReferenceDataService, requestFactory);
        }

        public IPromise<IDictionary<string, IDictionary<DateTime, IDictionary<string, object>>>> RequestHistoricalData(ICollection<string> tickers, IList<string> fields, DateTime startDate, DateTime endDate, PeriodicitySelection periodicitySelection)
        {
            return RequestHistoricalData(
                new HistoricalDataRequestFactory
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

        public IPromise<IDictionary<string,IDictionary<DateTime,IDictionary<string,object>>>> RequestHistoricalData(HistoricalDataRequestFactory requestFactory)
        {
            return HistoricalDataManager.Request(Session, Identity, ReferenceDataService, requestFactory);
        }

        public IPromise<TickerIntradayBarData> RequestIntradayBar(ICollection<string> tickers, DateTime startDateTime, DateTime endDateTime, EventType eventType, int interval)
        {
            return RequestIntradayBar(
                new IntradayBarRequestFactory
                {
                    Tickers = tickers,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    EventType = eventType,
                    Interval = interval
                });
        }

        public IPromise<TickerIntradayBarData> RequestIntradayBar(IntradayBarRequestFactory requestFactory)
        {
            return IntradayBarManager.Request(Session, Identity, ReferenceDataService, requestFactory);
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
                        eventArgs.GetMessages().ForEach(message => ServiceManager.Process(session, message, OnFailure));
                        break;

                    case Event.EventType.ADMIN:
                        ProcessAdminMessage(eventArgs);
                        break;

                    case Event.EventType.TOKEN_STATUS:
                        eventArgs.GetMessages().ForEach(message => TokenManager.ProcessTokenStatusEvent(session, message, OnFailure));
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
                        RaiseEvent(AdminStatus, new EventArgs<AdminStatus>(Bloomberg.AdminStatus.SlowConsumerWarning));
                        break;
                    case "SlowConsumerWarningCleared":
                        RaiseEvent(AdminStatus, new EventArgs<AdminStatus>(Bloomberg.AdminStatus.SlowConsumerWarningCleared));
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
                    IntraDayTickManager.Process(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.HistoricalDataResponse))
                    HistoricalDataManager.Process(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.ReferenceDataResponse))
                    ReferenceDataManager.Process(session, message, isPartialResponse, OnFailure);
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
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Bloomberg.SessionStatus.Started));
            if (MessageTypeNames.SessionTerminated.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Bloomberg.SessionStatus.Terminated));
            if (MessageTypeNames.SessionStartupFailure.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Bloomberg.SessionStatus.StartupFailure));
            if (MessageTypeNames.SessionConnectionUp.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Bloomberg.SessionStatus.ConnectionUp));
            if (MessageTypeNames.SessionConnectionDown.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new EventArgs<SessionStatus>(Bloomberg.SessionStatus.ConnectionDown));
        }
    }
}
