using System.Diagnostics;
using System.Text;
using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using JetBlack.Bloomberg.Authenticators;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requesters;
using JetBlack.Promises;

namespace JetBlack.Bloomberg
{
    public class BloombergWrapper
    {
        public event EventHandler<SessionStatusEventArgs> SessionStatus;
        public event EventHandler<AdminStatusEventArgs> AdminStatus;
        public event EventHandler<AuthenticationStatusEventArgs> AuthenticationStatus;

        public Session Session { get; private set; }
        private Service _mktDataService, _refDataService;

        public TokenManager TokenManager { get; private set; }
        private readonly ServiceManager _serviceManager = new ServiceManager();
        private readonly SubscriptionManager _subscriptionManager = new SubscriptionManager();
        private readonly ReferenceDataManager _referenceDataManager = new ReferenceDataManager();
        private readonly HistoricalDataManager _historicalDataManager = new HistoricalDataManager();
        private readonly IntradayBarManager _intradayBarManager = new IntradayBarManager();
        private readonly IntraDayTickManager _intraDayTickManager = new IntraDayTickManager();

        public IAuthenticator Authenticator { get; private set; }

        public BloombergWrapper(SessionOptions sessionOptions)
        {
            TokenManager = new TokenManager();
            Session = new Session(sessionOptions, HandleMessage);
        }

        public void Start()
        {
            lock (Session)
            {
                if (!Session.Start())
                    throw new Exception("Failed to start session");

                _mktDataService = OpenService("//blp/mktdata");
                _refDataService = OpenService("//blp/refdata");
            }
        }

        public void StartAsync()
        {
            Session.StartAsync();
        }

        public void AuthenticateAsync(IAuthenticator authenticator)
        {
            Authenticator = authenticator;

            _serviceManager.Request(Session, ServiceUris.AuthenticationService)
                .Then(service => Authenticator.Request(Session, service))
                .Then(isAuthenticated =>
                {
                    RaiseEvent(AuthenticationStatus, new AuthenticationStatusEventArgs(isAuthenticated));
                    return isAuthenticated ? Promise.Resolved() : Promise.Rejected(new ApplicationException("Authentication failed"));
                })
                .ThenAll(() => new[]
                {
                    _serviceManager.Request(Session, ServiceUris.ReferenceDataService).Then(service => Promise.Resolved(() => _refDataService = service)),
                    _serviceManager.Request(Session, ServiceUris.MarketDataService).Then(service => Promise.Resolved(() => _mktDataService = service))
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
            return _serviceManager.Open(Session, uri);
        }

        public IPromise<Service> RequestService(string uri)
        {
            return _serviceManager.Request(Session, uri);
        }

        public IObservable<TickerData> ToObservable(IEnumerable<string> tickers, IList<string> fields)
        {
            return _subscriptionManager.ToObservable(Session, tickers, fields);
        }

        public void RequestIntradayTick(ICollection<string> tickers, IEnumerable<EventType> eventTypes, DateTime startDateTime, DateTime endDateTime, Action<TickerIntradayTickData> onSuccess, Action<TickerResponseError> onFailure)
        {
            RequestIntradayTick(
                new IntradayTickRequester
                {
                    Tickers = tickers,
                    EventTypes = eventTypes,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime
                }, onSuccess, onFailure);
        }

        public void RequestIntradayTick(IntradayTickRequester request, Action<TickerIntradayTickData> onSuccess, Action<TickerResponseError> onFailure)
        {
            _intraDayTickManager.Request(Session, _refDataService, request, onSuccess, onFailure);
            
        }

        public void RequestReferenceData(Session session, Service refDataService, ReferenceDataRequester requester, Action<TickerData> onSuccess, Action<TickerSecurityError> onFailure)
        {
            _referenceDataManager.Request(Session, _refDataService, requester, onSuccess, onFailure);
        }

        public void RequestHistoricalData(ICollection<string> tickers, IList<string> fields, DateTime startDate, DateTime endDate, PeriodicitySelection periodicitySelection, Action<HistoricalTickerData> onSuccess, Action<TickerSecurityError> onFailure)
        {
            var requester = new HistoricalDataRequester
            {
                Tickers = tickers,
                Fields = fields,
                StartDate = startDate,
                EndDate = endDate,
                PeriodicitySelection = periodicitySelection,
                PeriodicityAdjustment = PeriodicityAdjustment.ACTUAL,
                NonTradingDayFillOption = NonTradingDayFillOption.ACTIVE_DAYS_ONLY,
                NonTradingDayFillMethod = NonTradingDayFillMethod.NIL_VALUE,
            };

            _historicalDataManager.Request(Session, _refDataService, requester, onSuccess, onFailure);
        }

        public void RequestIntradayBar(ICollection<string> tickers, DateTime startDateTime, DateTime endDateTime, EventType eventType, int interval, Action<TickerIntradayBarData> onSuccess, Action<TickerResponseError> onFailure)
        {
            var request =
                new IntradayBarRequester
                {
                    Tickers = tickers,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    EventType = eventType,
                    Interval = interval
                };
            _intradayBarManager.Request(Session, _refDataService, request, onSuccess, onFailure);
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
                        RaiseEvent(AdminStatus, new AdminStatusEventArgs(Bloomberg.AdminStatus.SlowConsumerWarning));
                        break;
                    case "SlowConsumerWarningCleared":
                        RaiseEvent(AdminStatus, new AdminStatusEventArgs(Bloomberg.AdminStatus.SlowConsumerWarningCleared));
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
                    _intradayBarManager.ProcessIntradayBarResponse(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.IntradayTickResponse))
                    _intraDayTickManager.ProcessIntradayTickResponse(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.HistoricalDataResponse))
                    _historicalDataManager.ProcessHistoricalDataResponse(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.ReferenceDataResponse))
                    _referenceDataManager.ProcessReferenceDataResponse(session, message, isPartialResponse, OnFailure);
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
                RaiseEvent(SessionStatus, new SessionStatusEventArgs(Bloomberg.SessionStatus.Started));
            if (MessageTypeNames.SessionTerminated.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new SessionStatusEventArgs(Bloomberg.SessionStatus.Terminated));
            if (MessageTypeNames.SessionStartupFailure.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new SessionStatusEventArgs(Bloomberg.SessionStatus.StartupFailure));
            if (MessageTypeNames.SessionConnectionUp.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new SessionStatusEventArgs(Bloomberg.SessionStatus.ConnectionUp));
            if (MessageTypeNames.SessionConnectionDown.Equals(message.MessageType))
                RaiseEvent(SessionStatus, new SessionStatusEventArgs(Bloomberg.SessionStatus.ConnectionDown));
        }
    }
}
