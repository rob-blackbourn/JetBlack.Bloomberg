using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JetBlack.Bloomberg
{
    public class BloombergWrapper
    {
        #region Events

        public event DataReceivedEventHandler OnDataReceived;
        public event HistoricalDataReceivedEventHandler OnHistoricalDataReceived;
        public event IntradayBarReceivedEventHandler OnIntradayBarReceived;
        public event IntradayTickDataReceivedEventHandler OnIntradayTickDataReceived;
        public event NotifyErrorResponseEventHandler OnNotifyErrorResponse;
        public event SessionStatusEventHandler OnSessionStatus;
        public event ServiceStatusEventHandler OnServiceStatus;
        public event AdminStatusEventHandler OnAdminStatus;
        public event SubscriptionStatusEventHandler OnSubscriptionStatus;
        public event FieldSubscriptionStatusEventHandler OnFieldSubscriptionStatus;
        public event ResponseStatusEventHandler OnResponseStatus;
        public event AuthenticationEventHandler OnAuthenticationStatus;
        public event AuthenticationErrorEventHandler OnAuthorisationError;

        #endregion

        #region Private Fields

        private Session session;
        private Service mktDataService, refDataService;
        private readonly Dictionary<CorrelationID, ICollection<string>> requestMap = new Dictionary<CorrelationID, ICollection<string>>();
        private IAuthenticator authenticator;
        private string uuid;

        private readonly Dictionary<CorrelationID, IList<IntradayTickData>> intradayTickDataCache = new Dictionary<CorrelationID, IList<IntradayTickData>>();
        private readonly Dictionary<CorrelationID, IList<IntradayBar>> intradayBarCache = new Dictionary<CorrelationID, IList<IntradayBar>>();

        private int lastCorrelationId;

        #endregion

        #region Element Names

        private static readonly Name CATEGORY = new Name("category");
        private static readonly Name CODE = new Name("code");
        private static readonly Name DESCRIPTION = new Name("description");
        private static readonly Name ERROR_INFO = new Name("errorInfo");
        private static readonly Name ERROR_CODE = new Name("errorCode");
        private static readonly Name EXCEPTIONS = new Name("exceptions");
        private static readonly Name FIELD_DATA = new Name("fieldData");
        private static readonly Name FIELD_EXCEPTIONS = new Name("fieldExceptions");
        private static readonly Name FIELD_ID = new Name("fieldId");
        private static readonly Name MESSAGE = new Name("message");
        private static readonly Name REASON = new Name("reason");
        private static readonly Name RESPONSE_ERROR = new Name("responseError");
        private static readonly Name SECURITY = new Name("security");
        private static readonly Name SECURITY_DATA = new Name("securityData");
        private static readonly Name SECURITY_ERROR = new Name("securityError");
        private static readonly Name SOURCE = new Name("source");
        private static readonly Name SUB_CATEGORY = new Name("subcategory");
        private static readonly Name TICK_DATA = new Name("tickData");

        private static readonly Name REFERENCE_DATA_RESPONSE = new Name("ReferenceDataResponse");
        private static readonly Name HISTORICAL_DATA_RESPONSE = new Name("HistoricalDataResponse");
        private static readonly Name INTRADAY_TICK_RESPONSE = new Name("IntradayTickResponse");
        private static readonly Name INTRADAY_BAR_RESPONSE = new Name("IntradayBarResponse");

        private static readonly Name AUTHORIZATION_FAILURE = new Name("AuthorizationFailure");
        private static readonly Name AUTHORIZATION_SUCCESS = new Name("AuthorizationSuccess");

        #endregion

        #region Constructors

        public BloombergWrapper()
            : this(null, 0, null, null)
        {
        }

        public BloombergWrapper(string serverHostname, int serverPort, string uuid)
            : this(serverHostname, serverPort, null, uuid)
        {
        }

        public BloombergWrapper(string serverHostname, int serverPort, string clientHostname, string uuid)
        {
            SessionOptions sessionOptions = new SessionOptions();
            this.uuid = uuid;
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

            session = new Session(sessionOptions, new Bloomberglp.Blpapi.EventHandler(HandleMessage));

            if (serverHostname == null)
            {
                authenticator = new PassingAuthenticator();
            }
            else
            {
#if NO_AUTHENTICATION
                authenticator = new PassingAuthenticator();
#else
                authenticator = new Authenticator(session, this, clientHostname, uuid);
#endif
            }

            lastCorrelationId = 0;
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            lock (session)
            {
                if (!session.Start())
                    throw new Exception("Failed to start session");

                if (!session.OpenService("//blp/mktdata"))
                    throw new Exception("Failed to open service \"//blp/mktdata\"");
                mktDataService = session.GetService("//blp/mktdata");

                if (!session.OpenService("//blp/refdata"))
                    throw new Exception("Failed to open service \"//blp/refdata\"");
                refDataService = session.GetService("//blp/refdata");
            }
#if NO_AUTHENTICATION
            authenticator.Authorise();
#endif
        }

        public void Stop()
        {
            lock (session)
                session.Stop();
        }

        public void Desubscribe(CorrelationID correlationId)
        {
            lock (session)
                session.Cancel(correlationId);
        }

        public void Desubscribe(IList<CorrelationID> correlators)
        {
            lock (session)
                session.Cancel(correlators);
        }

        public Dictionary<string, CorrelationID> Subscribe(IEnumerable<string> tickers, IList<string> fields)
        {
            if (authenticator.AuthenticationState != AuthenticationState.Succeeded)
            {
                NotifyAuthenticationResponse(false);
                return new Dictionary<string, CorrelationID>();
            }

            lock (session)
            {
                Dictionary<string, Subscription> subscriptions = new Dictionary<string, Subscription>();
                Dictionary<string, CorrelationID> realtimeRequestIds = new Dictionary<string, CorrelationID>();

                foreach (string ticker in tickers)
                {
                    //Trace.TraceInformation("Asking for ticker: {0}", ticker);

                    CorrelationID correlationId = new CorrelationID(++lastCorrelationId);
                    if (!subscriptions.ContainsKey(ticker))
                    {
                        subscriptions.Add(ticker, new Subscription(ticker, fields, correlationId));
                        realtimeRequestIds.Add(ticker, correlationId);
                    }
                    else
                    {
                        Trace.TraceInformation("Duplicate ticker requested: {0}", ticker);
                    }
                }
                session.Subscribe(new List<Subscription>(subscriptions.Values));
                return realtimeRequestIds;
            }
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
            if (authenticator.AuthenticationState != AuthenticationState.Succeeded)
            {
                NotifyAuthenticationResponse(false);
                return;
            }

            IEnumerable<Request> requests = requester.CreateRequests(refDataService);

            lock (session)
            {
                foreach (Request request in requests)
                {
                    CorrelationID correlationId = new CorrelationID(++lastCorrelationId);
                    if (requester.MapTickers) requestMap.Add(correlationId, requester.Tickers);
                    session.SendRequest(request, correlationId);
                }
            }
        }

        #endregion

        #region Private Methods

        private void HandleMessage(Event e, Session s)
        {
            try
            {
                switch (e.Type)
                {
                    case Event.EventType.PARTIAL_RESPONSE:
                    case Event.EventType.RESPONSE:
                        ProcessResponse(e, e.Type == Event.EventType.PARTIAL_RESPONSE);
                        break;

                    case Event.EventType.SUBSCRIPTION_DATA:
                        ProcessSubscription(e);
                        break;

                    case Event.EventType.SUBSCRIPTION_STATUS:
                        ProcessSubscriptionStatus(e);
                        break;

                    case Event.EventType.SESSION_STATUS:
                        ProcessSessionStatus(e);
                        break;

                    case Event.EventType.SERVICE_STATUS:
                        ProcessServiceStatus(e);
                        break;

                    case Event.EventType.ADMIN:
                        ProcessAdminMessage(e);
                        break;

                    default:
                        Trace.TraceWarning("Unhandled event type {0}", e.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleMessage: {0}", ex.Message);
            }
        }

        private void ProcessAuthorisationMessage(Message m)
        {
            if (m.MessageType.Equals(AUTHORIZATION_FAILURE))
            {
                OnAuthenticationStatus(false);
            }
            else if (m.MessageType.Equals(AUTHORIZATION_SUCCESS))
            {
                OnAuthenticationStatus(true);
            }
            else
            {
                OnAuthenticationStatus(false);
            }
        }

        private void ProcessAdminMessage(Event e)
        {
            if (e.IsValid)
            {
                foreach (Message m in e.GetMessages())
                {
                    switch (m.MessageType.ToString())
                    {
                        case "SlowConsumerWarning":
                            NotifyAdminStatus(AdminStatus.SlowConsumerWarning);
                            break;
                        case "SlowConsumerWarningCleared":
                            NotifyAdminStatus(AdminStatus.SlowConsumerWarningCleared);
                            break;
                        default:
                            Trace.TraceWarning("unknown message type {0}", m.MessageType.ToString());
                            break;
                    }
                }
            }
        }

        private void ProcessResponse(Event e, bool isPartialResponse)
        {
            if (!e.IsValid)
            {
                // TODO: What should we do here?
                return;
            }

            foreach (Message m in e.GetMessages())
            {
                if (m.MessageType.Equals(AUTHORIZATION_FAILURE) || m.MessageType.Equals(AUTHORIZATION_SUCCESS))
                    ProcessAuthorisationMessage(m);
                if (m.MessageType.Equals(INTRADAY_BAR_RESPONSE))
                    ProcessIntradayBarResponse(m, isPartialResponse);
                else if (m.MessageType.Equals(INTRADAY_TICK_RESPONSE))
                    ProcessIntradayTickResponse(m, isPartialResponse);
                else if (m.MessageType.Equals(HISTORICAL_DATA_RESPONSE))
                    ProcessHistoricalDataResponse(m, isPartialResponse);
                else if (m.MessageType.Equals(REFERENCE_DATA_RESPONSE))
                    ProcessReferenceDataResponse(m, isPartialResponse);
            }
        }

        private string ExtractTicker(CorrelationID correlationId, bool isPartialResponse)
        {
            string ticker;
            lock (session)
            {
                ticker = requestMap[correlationId].First();
                if (!isPartialResponse)
                    requestMap.Remove(correlationId);
            }
            return ticker;
        }

        private void ExtractTicker(CorrelationID correlationId, string ticker, bool isPartialResponse)
        {
            if (isPartialResponse) return;
            lock (session)
            {
                ICollection<string> tickers = requestMap[correlationId];
                tickers.Remove(ticker);
                if (requestMap.Count == 0) requestMap.Remove(correlationId);
            }
        }

        private IList<int> ExtractEids(Element eidDataElement)
        {
            List<int> eids = new List<int>();
            for (int i = 0; i < eidDataElement.NumValues; ++i)
            {
                int eid = eidDataElement.GetValueAsInt32(i);
                eids.Add(eid);
            }
            return eids;
        }

        private bool AssertResponseError(Message m, string name)
        {
            if (!m.HasElement(RESPONSE_ERROR)) return false;
            OnNotifyErrorResponse(name, m.MessageType.ToString(), string.Format("RESPONSE_ERROR: {0}", m.GetElement(RESPONSE_ERROR)));
            return true;
        }

        private Element ExtractEidElement(Element parent)
        {
            if (parent.HasElement("eidData")) return parent.GetElement("eidData");
            return null;
        }

        private void ProcessIntradayBarResponse(Message m, bool isPartialResponse)
        {
            string ticker = ExtractTicker(m.CorrelationID, isPartialResponse);
            if (AssertResponseError(m, ticker)) return;

            Element barData = m.GetElement("barData");
            Element barTickData = barData.GetElement("barTickData");

            if (!intradayBarCache.ContainsKey(m.CorrelationID))
                intradayBarCache.Add(m.CorrelationID, new List<IntradayBar>());
            IList<IntradayBar> intradayBars = intradayBarCache[m.CorrelationID];

            Element eidDataElement = ExtractEidElement(barData);

            for (int i = 0; i < barTickData.NumValues; ++i)
            {
                Element element = barTickData.GetValueAsElement(i);
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
                intradayBarCache.Remove(m.CorrelationID);
                if (authenticator.Permits(eidDataElement, m.Service))
                    NotifyIntradayBarReceived(ticker, intradayBars, ExtractEids(eidDataElement));
                else
                    NotifyAuthenticationError(ticker, m.MessageType.ToString(), ExtractEids(eidDataElement));
            }
        }

        private void ProcessIntradayTickResponse(Message m, bool isPartialResponse)
        {
            string ticker = ExtractTicker(m.CorrelationID, isPartialResponse);
            if (AssertResponseError(m, ticker)) return;

            Element tickData = m.GetElement("tickData");
            Element tickDataArray = tickData.GetElement("tickData");
            Element eidDataElement = ExtractEidElement(tickData);
            if (!intradayTickDataCache.ContainsKey(m.CorrelationID))
                intradayTickDataCache.Add(m.CorrelationID, new List<IntradayTickData>());
            IList<IntradayTickData> intradayTickData = intradayTickDataCache[m.CorrelationID];

            for (int i = 0; i < tickDataArray.NumValues; ++i)
            {
                Element item = tickDataArray.GetValueAsElement(i);
                string[] conditionCodes = (item.HasElement("conditionCodes") ? item.GetElementAsString("conditionCodes").Split(',') : null);
                string[] exchangeCodes = (item.HasElement("exchangeCode") ? item.GetElementAsString("exchangeCode").Split(',') : null);

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
                intradayTickDataCache.Remove(m.CorrelationID);
                List<int> missingEntitlements = new List<int>();
                if (authenticator.Permits(eidDataElement, m.Service))
                    NotifyIntradayTickDataReceived(ticker, intradayTickData, ExtractEids(eidDataElement));
                else
                    NotifyAuthenticationError(ticker, m.MessageType.ToString(), ExtractEids(eidDataElement));
            }
        }

        private void ProcessHistoricalDataResponse(Message m, bool isPartialResponse)
        {
            if (m.HasElement(RESPONSE_ERROR))
            {
                Trace.TraceInformation("RESPONSE_ERROR: {0}", m.GetElement(RESPONSE_ERROR));
                return;
            }

            Element securityDataArray = m.GetElement(SECURITY_DATA);

            for (int i = 0; i < securityDataArray.NumValues; ++i)
            {
                Element securityData = securityDataArray.GetElement(i);
                string ticker = securityData.GetValueAsString();

                if (securityDataArray.HasElement("securityError"))
                {
                    Element securityError = securityDataArray.GetElement("securityError");
                    NotifyResponseStatus(
                        ticker,
                        ResponseStatus.InvalidSecurity,
                        securityError.GetElement(SOURCE).GetValueAsString(),
                        securityError.GetElement(CATEGORY).GetValueAsString(),
                        securityError.GetElement(CODE).GetValueAsInt32(),
                        securityError.GetElement(SUB_CATEGORY).GetValueAsString(),
                        securityError.GetElement(MESSAGE).GetValueAsString());
                    continue;
                }

                Element fieldDataArray = securityDataArray.GetElement(FIELD_DATA);

                IDictionary<DateTime, IDictionary<string, object>> historicalMessageWrapper = new Dictionary<DateTime, IDictionary<string, object>>();

                for (int j = 0; j < fieldDataArray.NumValues; ++j)
                {
                    Dictionary<string, object> messageWrapper = new Dictionary<string, object>();
                    Element fieldData = fieldDataArray.GetValueAsElement(j);

                    for (int k = 0; k < fieldData.NumElements; ++k)
                    {
                        Element field = fieldData.GetElement(k);
                        string name = field.Name.ToString();
                        object value = field.GetFieldValue();
                        if (messageWrapper.ContainsKey(name))
                            messageWrapper[name] = value;
                        else
                            messageWrapper.Add(name, value);
                    }

                    if (messageWrapper.ContainsKey("date"))
                    {
                        DateTime date = (DateTime)messageWrapper["date"];
                        messageWrapper.Remove("date");
                        historicalMessageWrapper.Add(date, messageWrapper);
                    }
                }

                NotifyHistoricalDataReceived(ticker, historicalMessageWrapper);
            }
        }

        private void ProcessReferenceDataResponse(Message m, bool isPartialResponse)
        {
            if (m.HasElement(RESPONSE_ERROR))
            {
                Trace.TraceInformation("RESPONSE_ERROR: {0}", m.GetElement(RESPONSE_ERROR));
                return;
            }

            Element securities = m.GetElement(SECURITY_DATA);
            for (int i = 0; i < securities.NumValues; ++i)
            {
                Element security = securities.GetValueAsElement(i);
                string ticker = security.GetElementAsString(SECURITY);

                if (security.HasElement(SECURITY_ERROR))
                {
                    Element securityError = security.GetElement(SECURITY_ERROR);
                    NotifyResponseStatus(
                        ticker,
                        ResponseStatus.InvalidSecurity,
                        securityError.GetElement(SOURCE).GetValueAsString(),
                        securityError.GetElement(CATEGORY).GetValueAsString(),
                        securityError.GetElement(CODE).GetValueAsInt32(),
                        securityError.GetElement(SUB_CATEGORY).GetValueAsString(),
                        securityError.GetElement(MESSAGE).GetValueAsString());
                    continue;
                }

                Dictionary<string, object> messageWrapper = new Dictionary<string, object>();
                Element fields = security.GetElement(FIELD_DATA);
                for (int j = 0; j < fields.NumElements; ++j)
                {
                    Element field = fields.GetElement(j);
                    string name = field.Name.ToString();
                    object value = field.GetFieldValue();
                    if (messageWrapper.ContainsKey(name))
                        messageWrapper[name] = value;
                    else
                        messageWrapper.Add(name, value);
                }

                NotifyDataReceived(ticker, messageWrapper);
            }
        }

        private void ProcessSubscription(Event e)
        {
            if (e.IsValid)
            {
                foreach (Message m in e.GetMessages())
                {
                    Dictionary<string, object> messageWrapper = new Dictionary<string, object>();

                    foreach (Element field in m.Elements)
                    {
                        if (field.IsNull)
                            continue;

                        string name = field.Name.ToString();
                        object value = field.GetFieldValue();

                        if (messageWrapper.ContainsKey(name))
                            messageWrapper[name] = value;
                        else
                            messageWrapper.Add(name, value);
                    }

                    NotifyDataReceived(m.TopicName, messageWrapper);
                }
            }
        }

        private void ProcessSubscriptionStatus(Event e)
        {
            if (e.IsValid)
            {
                foreach (Message m in e.GetMessages())
                {
                    if (m.HasElement(REASON))
                    {
                        Element reason = m.GetElement(REASON);

                        SubscriptionStatus status = SubscriptionStatus.None;

                        string messageTypeString = m.MessageType.ToString();
                        switch (messageTypeString)
                        {
                            case "SubscriptionFailure":
                                status = SubscriptionStatus.Failure;
                                break;
                            case "SubscriptionTerminated":
                                status = SubscriptionStatus.Terminated;
                                break;
                            default:
                                Trace.TraceWarning("unknown subscription status message: {0}", messageTypeString);
                                break;
                        }

                        if (status == SubscriptionStatus.None)
                        {
                            Trace.TraceWarning("unknown subscription status message type: {0}", m);
                        }
                        else
                        {
                            NotifySubscriptionStatus(
                                m.TopicName,
                                status,
                                reason.GetElement(SOURCE).GetValueAsString(),
                                reason.GetElement(CATEGORY).GetValueAsString(),
                                reason.GetElement(ERROR_CODE).GetValueAsInt32(),
                                reason.GetElement(DESCRIPTION).GetValueAsString());
                        }
                    }

                    if (m.HasElement(EXCEPTIONS))
                    {
                        // Field subscription failures
                        Element exceptions = m.GetElement("exceptions");

                        FieldSubscriptionStatus status = FieldSubscriptionStatus.None;
                        switch (m.MessageType.ToString())
                        {
                            case "SubscriptionStarted":
                                status = FieldSubscriptionStatus.Started;
                                break;
                        }

                        if (status == FieldSubscriptionStatus.None)
                        {
                            Trace.TraceWarning("unknown subscription status message type: {0}", m);
                        }
                        else
                        {
                            for (int i = 0; i < exceptions.NumValues; ++i)
                            {
                                Element exception = exceptions.GetValueAsElement(i);
                                string fieldId = exception.GetElement(FIELD_ID).GetValueAsString();
                                Element reason = exception.GetElement(REASON);
                                string source = reason.GetElement(SOURCE).GetValueAsString();
                                string category = reason.GetElement(CATEGORY).GetValueAsString();
                                object objSubCategory = reason.GetElement(SUB_CATEGORY);
                                string subcategory = (objSubCategory != null ? objSubCategory.ToString() : string.Empty);
                                int errorCode = reason.GetElement(ERROR_CODE).GetValueAsInt32();
                                string desc = reason.GetElement(DESCRIPTION).GetValueAsString();

                                NotifyFieldSubscriptionStatus(
                                    m.TopicName,
                                    status,
                                    fieldId,
                                    source,
                                    category,
                                    subcategory,
                                    errorCode,
                                    desc);
                            }
                        }
                    }
                }
            }
        }

        private void ProcessServiceStatus(Event e)
        {
            if (e.IsValid)
            {
                foreach (Message m in e.GetMessages())
                {
                    switch (m.MessageType.ToString())
                    {
                        case "ServiceOpened":
                            NotifyServiceStatus(m.TopicName, ServiceStatus.Opened);
                            break;
                        case "ServiceClosed":
                            NotifyServiceStatus(m.TopicName, ServiceStatus.Closed);
                            break;
                        default:
                            Trace.TraceWarning("unhandled message type {0}", m.MessageType.ToString());
                            break;
                    }
                }
            }
        }

        private void ProcessSessionStatus(Event e)
        {
            if (e.IsValid)
            {
                foreach (Message m in e.GetMessages())
                {
                    switch (m.MessageType.ToString())
                    {
                        case "SessionStarted":
                            NotifySessionStatus(SessionStatus.Started);
                            break;
                        case "SessionStopped":
                            NotifySessionStatus(SessionStatus.Stopped);
                            break;
                        default:
                            Trace.TraceWarning("unknown message type {0}", m.MessageType.ToString());
                            break;
                    }
                }
            }
        }

        private void NotifyAuthenticationResponse(bool isSuccess)
        {
            if (OnAuthenticationStatus != null)
                OnAuthenticationStatus(isSuccess);
        }

        private void NotifyErrorResponse(string name, string messageType, string responseError)
        {
            if (OnNotifyErrorResponse != null)
                OnNotifyErrorResponse(name, messageType, responseError);
        }

        private void NotifyAuthenticationError(string ticker, string messageType, IEnumerable<int> eids)
        {
            if (OnAuthorisationError != null)
                OnAuthorisationError(ticker, messageType, eids);
        }

        private void NotifyIntradayTickDataReceived(string name, IList<IntradayTickData> intradayTickDataList, IList<int> entitlementIds)
        {
            if (OnIntradayTickDataReceived != null)
                OnIntradayTickDataReceived(name, intradayTickDataList, entitlementIds);
        }

        private void NotifyIntradayBarReceived(string name, IList<IntradayBar> message, IEnumerable<int> eids)
        {
            if (OnIntradayBarReceived != null)
                OnIntradayBarReceived(name, message, eids);
        }

        private void NotifyHistoricalDataReceived(string name, IDictionary<DateTime, IDictionary<string, object>> message)
        {
            if (OnHistoricalDataReceived != null)
                OnHistoricalDataReceived(name, message);
        }

        private void NotifyDataReceived(string name, IDictionary<string, object> message)
        {
            if (OnDataReceived != null)
                OnDataReceived(name, message);
        }

        private void NotifySessionStatus(SessionStatus status)
        {
            if (OnSessionStatus != null)
                OnSessionStatus(status);
        }

        private void NotifyServiceStatus(string name, ServiceStatus status)
        {
            if (OnServiceStatus != null)
                OnServiceStatus(name, status);
        }

        private void NotifyAdminStatus(AdminStatus status)
        {
            if (OnAdminStatus != null)
                OnAdminStatus(status);
        }

        private void NotifySubscriptionStatus(string name, SubscriptionStatus status, string source, string category, int errorCode, string description)
        {
            if (OnSubscriptionStatus != null)
                OnSubscriptionStatus(name, status, source, category, errorCode, description);
        }

        private void NotifyFieldSubscriptionStatus(string name, FieldSubscriptionStatus status, string fieldId, string source, string category, string subCategory, int errorCode, string description)
        {
            if (OnFieldSubscriptionStatus != null)
                OnFieldSubscriptionStatus(name, fieldId, status, source, category, subCategory, errorCode, description);
        }

        private void NotifyResponseStatus(string name, ResponseStatus status, string source, string category, int code, string subCategory, string message)
        {
            if (OnResponseStatus != null)
                OnResponseStatus(name, status, source, category, code, subCategory, message);
        }

        #endregion
    }
}
