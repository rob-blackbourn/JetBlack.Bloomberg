using System.Net;
using System.Net.Sockets;
using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBlack.Bloomberg.Authenticators;
using JetBlack.Bloomberg.Messages;

namespace JetBlack.Bloomberg
{
    public class BloombergWrapper
    {
        public event EventHandler<DataReceivedEventArgs> OnDataReceived;
        public event EventHandler<HistoricalDataReceivedEventArgs> OnHistoricalDataReceived;
        public event EventHandler<IntradayBarReceivedEventArgs> OnIntradayBarReceived;
        public event EventHandler<IntradayTickDataReceivedEventArgs> OnIntradayTickDataReceived;
        public event EventHandler<ErrorResponseEventArgs> OnNotifyErrorResponse;
        public event EventHandler<SessionStatusEventArgs> OnSessionStatus;
        public event EventHandler<AdminStatusEventArgs> OnAdminStatus;
        public event EventHandler<SubscriptionStatusEventArgs> OnSubscriptionStatus;
        public event EventHandler<FieldSubscriptionStatusEventArgs> OnFieldSubscriptionStatus;
        public event EventHandler<ResponseStatusEventArgs> OnResponseStatus;
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

        public void RequestReferenceData(ICollection<string> tickers, IEnumerable<string> fields)
        {
            Request(
                new ReferenceDataRequester
                {
                    Tickers = tickers,
                    Fields = fields
                });
        }

        public void RequestHistoricalData(ICollection<string> tickers, IList<string> fields, DateTime startDate, DateTime endDate, PeriodicitySelection periodicitySelection)
        {
            Request(
                new HistoricalDataRequester
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

        public void RequestIntradayBar(ICollection<string> tickers, DateTime startDateTime, DateTime endDateTime, EventType eventType, int interval)
        {
            Request(
                new IntradayBarRequester
                {
                    Tickers = tickers,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    EventType = eventType,
                    Interval = interval
                });
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
                    ProcessIntradayBarResponse(message, isPartialResponse);
                else if (message.MessageType.Equals(MessageTypeNames.IntradayTickResponse))
                    ProcessIntradayTickResponse(message, isPartialResponse);
                else if (message.MessageType.Equals(MessageTypeNames.HistoricalDataResponse))
                    ProcessHistoricalDataResponse(message, isPartialResponse);
                else if (message.MessageType.Equals(MessageTypeNames.ReferenceDataResponse))
                    ProcessReferenceDataResponse(message, isPartialResponse);
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

        private void ExtractTicker(CorrelationID correlationId, string ticker, bool isPartialResponse)
        {
            if (isPartialResponse) return;
            lock (_session)
            {
                ICollection<string> tickers = _requestMap[correlationId];
                tickers.Remove(ticker);
                if (_requestMap.Count == 0) _requestMap.Remove(correlationId);
            }
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

        private void ProcessIntradayBarResponse(Message message, bool isPartialResponse)
        {
            var ticker = ExtractTicker(message.CorrelationID, isPartialResponse);
            if (AssertResponseError(message, ticker)) return;

            var barData = message.GetElement("barData");
            var barTickData = barData.GetElement("barTickData");

            if (!_intradayBarCache.ContainsKey(message.CorrelationID))
                _intradayBarCache.Add(message.CorrelationID, new List<IntradayBar>());
            var intradayBars = _intradayBarCache[message.CorrelationID];

            var eidDataElement = ExtractEidElement(barData);

            for (var i = 0; i < barTickData.NumValues; ++i)
            {
                var element = barTickData.GetValueAsElement(i);
                intradayBars.Add(
                    new IntradayBar
                    {
                        Time = element.GetElementAsDatetime("time").ToDateTime(),
                        Open = element.GetElementAsFloat64("open"),
                        High = element.GetElementAsFloat64("high"),
                        Low = element.GetElementAsFloat64("low"),
                        Close = element.GetElementAsFloat64("close"),
                        NumEvents = element.GetElementAsInt32("numEvents"),
                        Volume = element.GetElementAsInt64("volume")
                    });
            }

            if (!isPartialResponse)
            {
                _intradayBarCache.Remove(message.CorrelationID);
                if (_authenticator.Permits(eidDataElement, message.Service))
                    RaiseEvent(OnIntradayBarReceived, new IntradayBarReceivedEventArgs(ticker, intradayBars, ExtractEids(eidDataElement)));
                else
                    RaiseEvent(OnAuthorisationError, new AuthorisationErrorEventArgs(ticker, message.MessageType.ToString(), ExtractEids(eidDataElement)));
            }
        }

        private void ProcessIntradayTickResponse(Message m, bool isPartialResponse)
        {
            var ticker = ExtractTicker(m.CorrelationID, isPartialResponse);
            if (AssertResponseError(m, ticker)) return;

            var tickData = m.GetElement("tickData");
            var tickDataArray = tickData.GetElement("tickData");
            var eidDataElement = ExtractEidElement(tickData);
            if (!_intradayTickDataCache.ContainsKey(m.CorrelationID))
                _intradayTickDataCache.Add(m.CorrelationID, new List<IntradayTickData>());
            var intradayTickData = _intradayTickDataCache[m.CorrelationID];

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
                _intradayTickDataCache.Remove(m.CorrelationID);
                if (_authenticator.Permits(eidDataElement, m.Service))
                    RaiseEvent(OnIntradayTickDataReceived, new IntradayTickDataReceivedEventArgs(ticker, intradayTickData, ExtractEids(eidDataElement)));
                else
                    RaiseEvent(OnAuthorisationError, new AuthorisationErrorEventArgs(ticker, m.MessageType.ToString(), ExtractEids(eidDataElement)));
            }
        }

        private void ProcessHistoricalDataResponse(Message message, bool isPartialResponse)
        {
            if (message.HasElement(ElementNames.ResponseError))
            {
                return;
            }

            var securityDataArray = message.GetElement(ElementNames.SecurityData);

            for (var i = 0; i < securityDataArray.NumValues; ++i)
            {
                var securityData = securityDataArray.GetElement(i);
                var ticker = securityData.GetValueAsString();

                if (securityDataArray.HasElement("securityError"))
                {
                    var securityError = securityDataArray.GetElement("securityError");
                    RaiseEvent(OnResponseStatus, new ResponseStatusEventArgs(
                        ticker,
                        ResponseStatus.InvalidSecurity,
                        securityError.GetElement(ElementNames.Source).GetValueAsString(),
                        securityError.GetElement(ElementNames.Category).GetValueAsString(),
                        securityError.GetElement(ElementNames.Code).GetValueAsInt32(),
                        securityError.GetElement(ElementNames.SubCategory).GetValueAsString(),
                        securityError.GetElement(ElementNames.Message).GetValueAsString()));
                    continue;
                }

                var fieldDataArray = securityDataArray.GetElement(ElementNames.FieldData);

                var historicalMessageWrapper = new Dictionary<DateTime, IDictionary<string, object>>();

                for (var j = 0; j < fieldDataArray.NumValues; ++j)
                {
                    var messageWrapper = new Dictionary<string, object>();
                    var fieldData = fieldDataArray.GetValueAsElement(j);

                    for (var k = 0; k < fieldData.NumElements; ++k)
                    {
                        var field = fieldData.GetElement(k);
                        var name = field.Name.ToString();
                        var value = field.GetFieldValue();
                        if (messageWrapper.ContainsKey(name))
                            messageWrapper[name] = value;
                        else
                            messageWrapper.Add(name, value);
                    }

                    if (messageWrapper.ContainsKey("date"))
                    {
                        var date = (DateTime)messageWrapper["date"];
                        messageWrapper.Remove("date");
                        historicalMessageWrapper.Add(date, messageWrapper);
                    }
                }

                RaiseEvent(OnHistoricalDataReceived, new HistoricalDataReceivedEventArgs(ticker, historicalMessageWrapper));
            }
        }

        private void ProcessReferenceDataResponse(Message m, bool isPartialResponse)
        {
            if (m.HasElement(ElementNames.ResponseError))
            {
                return;
            }

            var securities = m.GetElement(ElementNames.SecurityData);
            for (var i = 0; i < securities.NumValues; ++i)
            {
                var security = securities.GetValueAsElement(i);
                var ticker = security.GetElementAsString(ElementNames.Security);

                if (security.HasElement(ElementNames.SecurityError))
                {
                    var securityError = security.GetElement(ElementNames.SecurityError);
                    RaiseEvent(OnResponseStatus, new ResponseStatusEventArgs(
                        ticker,
                        ResponseStatus.InvalidSecurity,
                        securityError.GetElement(ElementNames.Source).GetValueAsString(),
                        securityError.GetElement(ElementNames.Category).GetValueAsString(),
                        securityError.GetElement(ElementNames.Code).GetValueAsInt32(),
                        securityError.GetElement(ElementNames.SubCategory).GetValueAsString(),
                        securityError.GetElement(ElementNames.Message).GetValueAsString()));
                    continue;
                }

                var messageWrapper = new Dictionary<string, object>();
                var fields = security.GetElement(ElementNames.FieldData);
                for (var j = 0; j < fields.NumElements; ++j)
                {
                    var field = fields.GetElement(j);
                    var name = field.Name.ToString();
                    var value = field.GetFieldValue();
                    if (messageWrapper.ContainsKey(name))
                        messageWrapper[name] = value;
                    else
                        messageWrapper.Add(name, value);
                }

                RaiseEvent(OnDataReceived, new DataReceivedEventArgs(ticker, messageWrapper));
            }
        }

        private void ProcessSubscriptionStatus(Event e)
        {
            if (e.IsValid)
            {
                foreach (var m in e.GetMessages())
                {
                    if (m.HasElement(ElementNames.Reason))
                    {
                        var reason = m.GetElement(ElementNames.Reason);

                        var status = SubscriptionStatus.None;

                        var messageTypeString = m.MessageType.ToString();
                        switch (messageTypeString)
                        {
                            case "SubscriptionFailure":
                                status = SubscriptionStatus.Failure;
                                break;
                            case "SubscriptionTerminated":
                                status = SubscriptionStatus.Terminated;
                                break;
                        }

                        if (status != SubscriptionStatus.None)
                        {
                            RaiseEvent(OnSubscriptionStatus, new SubscriptionStatusEventArgs(
                                m.TopicName,
                                status,
                                reason.GetElement(ElementNames.Source).GetValueAsString(),
                                reason.GetElement(ElementNames.Category).GetValueAsString(),
                                reason.GetElement(ElementNames.ErrorCode).GetValueAsInt32(),
                                reason.GetElement(ElementNames.Description).GetValueAsString()));
                        }
                    }

                    if (m.HasElement(ElementNames.Exceptions))
                    {
                        // Field subscription failures
                        var exceptions = m.GetElement("exceptions");

                        var status = FieldSubscriptionStatus.None;
                        switch (m.MessageType.ToString())
                        {
                            case "SubscriptionStarted":
                                status = FieldSubscriptionStatus.Started;
                                break;
                        }

                        if (status != FieldSubscriptionStatus.None)
                        {
                            for (var i = 0; i < exceptions.NumValues; ++i)
                            {
                                var exception = exceptions.GetValueAsElement(i);
                                var fieldId = exception.GetElement(ElementNames.FieldId).GetValueAsString();
                                var reason = exception.GetElement(ElementNames.Reason);
                                var source = reason.GetElement(ElementNames.Source).GetValueAsString();
                                var category = reason.GetElement(ElementNames.Category).GetValueAsString();
                                var objSubCategory = reason.GetElement(ElementNames.SubCategory);
                                var subcategory = (objSubCategory != null ? objSubCategory.ToString() : string.Empty);
                                var errorCode = reason.GetElement(ElementNames.ErrorCode).GetValueAsInt32();
                                var desc = reason.GetElement(ElementNames.Description).GetValueAsString();

                                RaiseEvent(OnFieldSubscriptionStatus, new FieldSubscriptionStatusEventArgs(
                                    m.TopicName,
                                    fieldId,
                                    status,
                                    source,
                                    category,
                                    subcategory,
                                    errorCode,
                                    desc));
                            }
                        }
                    }
                }
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
