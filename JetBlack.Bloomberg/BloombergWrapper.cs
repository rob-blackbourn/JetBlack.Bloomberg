using System.Net;
using System.Net.Sockets;
using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBlack.Bloomberg.Authenticators;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Requesters;

namespace JetBlack.Bloomberg
{
    public class BloombergWrapper
    {
        public event EventHandler<IntradayTickDataReceivedEventArgs> OnIntradayTickDataReceived;
        public event EventHandler<ErrorResponseEventArgs> OnNotifyErrorResponse;
        public event EventHandler<SessionStatusEventArgs> OnSessionStatus;
        public event EventHandler<AdminStatusEventArgs> OnAdminStatus;
        public event EventHandler<AuthenticationStatusEventArgs> OnAuthenticationStatus;
        public event EventHandler<AuthorisationErrorEventArgs> OnAuthorisationError;

        private readonly Session _session;
        private Service _mktDataService, _refDataService;
        private readonly Dictionary<CorrelationID, ICollection<string>> _requestMap = new Dictionary<CorrelationID, ICollection<string>>();
        private readonly IAuthenticator _authenticator;

        private readonly Dictionary<CorrelationID, IList<IntradayTickData>> _intradayTickDataCache = new Dictionary<CorrelationID, IList<IntradayTickData>>();
        private readonly Dictionary<CorrelationID, IList<IntradayBar>> _intradayBarCache = new Dictionary<CorrelationID, IList<IntradayBar>>();

        private readonly TokenManager _tokenManager = new TokenManager();
        private readonly ServiceManager _serviceManager = new ServiceManager();
        private readonly SubscriptionManager _subscriptionManager = new SubscriptionManager();
        private readonly ReferenceDataManager _referenceDataManager = new ReferenceDataManager();
        private readonly HistoricalDataManager _historicalDataManager = new HistoricalDataManager();
        private readonly IntradayBarManager _intradayBarManager = new IntradayBarManager();

        private int _lastCorrelationId;

        public BloombergWrapper(string serverHostname, int serverPort, string uuid, AuthenticationMethod authenticationMethod)
        {
            var sessionOptions = new SessionOptions();

            if (serverHostname == null)
            {
                sessionOptions.ClientMode = SessionOptions.ClientModeType.DAPI;
            }
            else
            {
                sessionOptions.ClientMode = SessionOptions.ClientModeType.SAPI;
                sessionOptions.ServerHost = serverHostname;
                sessionOptions.ServerPort = serverPort;
            }

            _session = new Session(sessionOptions, HandleMessage);
            var identity = _session.CreateIdentity();

            switch (authenticationMethod)
            {
                case AuthenticationMethod.Sapi:
                    var ipEntry = Dns.GetHostEntry(String.Empty);
                    var ipAddresses = ipEntry.AddressList;
                    var ipAddress = ipAddresses.First(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                    _authenticator = new SapiAuthenticator(identity, ipAddress, uuid);
                    break;
                case AuthenticationMethod.Bpipe:
                    _authenticator = new BpipeAuthenticator(identity, _tokenManager);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("authenticationMethod");
            }

            _authenticator = new BpipeAuthenticator(_session.CreateIdentity(), _tokenManager);

            _lastCorrelationId = 0;
        }

        #region Public Methods

        public void Start()
        {
            lock (_session)
            {
                if (!_session.Start())
                    throw new Exception("Failed to start session");

                _mktDataService = OpenService("//blp/mktdata");
                _refDataService = OpenService("//blp/refdata");
            }
#if NO_AUTHENTICATION
            authenticator.Authorise();
#endif
        }

        public void StartAsync()
        {
            _session.StartAsync();
        }

        public void Stop()
        {
            lock (_session)
                _session.Stop();
        }

        public string GenerateToken()
        {
            return _tokenManager.GenerateToken(_session);
        }

        public void GenerateToken(Action<SessionEventArgs<TokenGenerationSuccessEventArgs>> onSuccess, Action<SessionEventArgs<TokenGenerationFailureEventArgs>> onFailure)
        {
            _tokenManager.GenerateToken(_session, onSuccess, onFailure);
        }

        public Service OpenService(string uri)
        {
            return _serviceManager.Open(_session, uri);
        }

        public void OpenService(string uri, Action<SessionEventArgs<ServiceOpenedEventArgs>> onSuccess, Action<SessionEventArgs<ServiceOpenFailureEventArgs>> onFailure)
        {
            _serviceManager.Open(_session, uri, onSuccess, onFailure);
        }

        public IObservable<SessionEventArgs<DataReceivedEventArgs>> ToObservable(IEnumerable<string> tickers, IList<string> fields)
        {
            return _subscriptionManager.ToObservable(_session, tickers, fields);
        }

        #region Helper Methods

        public void RequestIntradayTick(ICollection<string> tickers, IEnumerable<EventType> eventTypes, DateTime startDateTime, DateTime endDateTime)
        {
            Request(
                new IntradayTickRequester
                {
                    Tickers = tickers,
                    EventTypes = eventTypes,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime
                });
        }


        public void RequestReferenceData(Session session, Service refDataService, ReferenceDataRequester requester, Action<TickerData> onSuccess, Action<TickerSecurityError> onFailure)
        {
            _referenceDataManager.Request(_session, _refDataService, requester, onSuccess, onFailure);
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

            _historicalDataManager.Request(_session, _refDataService, requester, onSuccess, onFailure);
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
            _intradayBarManager.Request(_session, _refDataService, request, onSuccess, onFailure);
        }

        #endregion

        public void Request(Requester requester)
        {
            var requests = requester.CreateRequests(_refDataService);

            lock (_session)
            {
                foreach (var request in requests)
                {
                    var correlationId = new CorrelationID(++_lastCorrelationId);
                    if (requester.MapTickers) _requestMap.Add(correlationId, requester.Tickers);
                    _session.SendRequest(request, correlationId);
                }
            }
        }

        #endregion

        #region Private Methods

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
                        eventArgs.ForEach(message => _subscriptionManager.ProcessSubscriptionData(session, message));
                        break;

                    case Event.EventType.SUBSCRIPTION_STATUS:
                        eventArgs.ForEach(message => _subscriptionManager.ProcessSubscriptionStatus(session, message));
                        break;

                    case Event.EventType.SESSION_STATUS:
                        ProcessSessionStatus(eventArgs);
                        break;

                    case Event.EventType.SERVICE_STATUS:
                        eventArgs.GetMessages().ForEach(message => _serviceManager.ProcessServiceStatusEvent(session, message, OnFailure));
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

        private void ProcessAuthorisationMessage(Message message)
        {
            if (message.MessageType.Equals(MessageTypeNames.AuthorizationFailure))
                RaiseEvent(OnAuthenticationStatus, new AuthenticationStatusEventArgs(false));
            else if (message.MessageType.Equals(MessageTypeNames.AuthorizationSuccess))
                RaiseEvent(OnAuthenticationStatus, new AuthenticationStatusEventArgs(true));
            else
                RaiseEvent(OnAuthenticationStatus, new AuthenticationStatusEventArgs(false));
        }

        private void ProcessAdminMessage(Event e)
        {
            if (!e.IsValid) return;

            foreach (var message in e.GetMessages())
            {
                switch (message.MessageType.ToString())
                {
                    case "SlowConsumerWarning":
                        RaiseEvent(OnAdminStatus, new AdminStatusEventArgs(AdminStatus.SlowConsumerWarning));
                        break;
                    case "SlowConsumerWarningCleared":
                        RaiseEvent(OnAdminStatus, new AdminStatusEventArgs(AdminStatus.SlowConsumerWarningCleared));
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
                    ProcessAuthorisationMessage(message);
                if (message.MessageType.Equals(MessageTypeNames.IntradayBarResponse))
                    _intradayBarManager.ProcessIntradayBarResponse(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.IntradayTickResponse))
                    ProcessIntradayTickResponse(session, message, isPartialResponse);
                else if (message.MessageType.Equals(MessageTypeNames.HistoricalDataResponse))
                    _historicalDataManager.ProcessHistoricalDataResponse(session, message, isPartialResponse, OnFailure);
                else if (message.MessageType.Equals(MessageTypeNames.ReferenceDataResponse))
                    _referenceDataManager.ProcessReferenceDataResponse(session, message, isPartialResponse, OnFailure);
            }
        }

        private string ExtractTicker(CorrelationID correlationId, bool isPartialResponse)
        {
            string ticker;
            lock (_session)
            {
                ticker = _requestMap[correlationId].First();
                if (!isPartialResponse)
                    _requestMap.Remove(correlationId);
            }
            return ticker;
        }

        private static IList<int> ExtractEids(Element eidDataElement)
        {
            var eids = new List<int>();
            for (var i = 0; i < eidDataElement.NumValues; ++i)
            {
                var eid = eidDataElement.GetValueAsInt32(i);
                eids.Add(eid);
            }
            return eids;
        }

        private bool AssertResponseError(Message m, string name)
        {
            if (!m.HasElement(ElementNames.ResponseError)) return false;
            RaiseEvent(OnNotifyErrorResponse, new ErrorResponseEventArgs(name, m.MessageType.ToString(), string.Format("RESPONSE_ERROR: {0}", m.GetElement(ElementNames.ResponseError))));
            return true;
        }

        private void RaiseEvent<T>(EventHandler<T> handler, T args) where T:EventArgs
        {
            if (handler != null)
                handler(this, args);
        }

        private static Element ExtractEidElement(Element parent)
        {
            return parent.HasElement("eidData") ? parent.GetElement("eidData") : null;
        }

        private void ProcessIntradayTickResponse(Session session, Message message, bool isPartialResponse)
        {
            var ticker = ExtractTicker(message.CorrelationID, isPartialResponse);
            if (AssertResponseError(message, ticker)) return;

            var tickData = message.GetElement("tickData");
            var tickDataArray = tickData.GetElement("tickData");
            var eidDataElement = ExtractEidElement(tickData);
            if (!_intradayTickDataCache.ContainsKey(message.CorrelationID))
                _intradayTickDataCache.Add(message.CorrelationID, new List<IntradayTickData>());
            var intradayTickData = _intradayTickDataCache[message.CorrelationID];

            for (var i = 0; i < tickDataArray.NumValues; ++i)
            {
                var item = tickDataArray.GetValueAsElement(i);
                var conditionCodes = (item.HasElement("conditionCodes") ? item.GetElementAsString("conditionCodes").Split(',') : null);
                var exchangeCodes = (item.HasElement("exchangeCode") ? item.GetElementAsString("exchangeCode").Split(',') : null);

                intradayTickData.Add(
                    new IntradayTickData
                    {
                        Time = item.GetElementAsDatetime("time").ToDateTime(),
                        EventType = (EventType)Enum.Parse(typeof(EventType), item.GetElementAsString("type"), true),
                        Value = item.GetElementAsFloat64("value"),
                        Size = item.GetElementAsInt32("size"),
                        ConditionCodes = conditionCodes,
                        ExchangeCodes = exchangeCodes
                    });
            }

            if (!isPartialResponse)
            {
                _intradayTickDataCache.Remove(message.CorrelationID);
                if (_authenticator.Permits(eidDataElement, message.Service))
                    RaiseEvent(OnIntradayTickDataReceived, new IntradayTickDataReceivedEventArgs(ticker, intradayTickData, ExtractEids(eidDataElement)));
                else
                    RaiseEvent(OnAuthorisationError, new AuthorisationErrorEventArgs(ticker, message.MessageType.ToString(), ExtractEids(eidDataElement)));
            }
        }

        private void ProcessSessionStatus(Event e)
        {
            if (e.IsValid)
            {
                foreach (var m in e.GetMessages())
                {
                    switch (m.MessageType.ToString())
                    {
                        case "SessionStarted":
                            RaiseEvent(OnSessionStatus, new SessionStatusEventArgs(SessionStatus.Started));
                            break;
                        case "SessionStopped":
                            RaiseEvent(OnSessionStatus, new SessionStatusEventArgs(SessionStatus.Stopped));
                            break;
                    }
                }
            }
        }

        #endregion
    }
}
