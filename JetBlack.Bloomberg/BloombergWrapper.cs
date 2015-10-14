﻿using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JetBlack.Bloomberg
{
    public class BloombergWrapper
    {
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

        private readonly Session _session;
        private Service _mktDataService, _refDataService;
        private readonly Dictionary<CorrelationID, ICollection<string>> _requestMap = new Dictionary<CorrelationID, ICollection<string>>();
        private readonly IAuthenticator _authenticator;

        private readonly Dictionary<CorrelationID, IList<IntradayTickData>> _intradayTickDataCache = new Dictionary<CorrelationID, IList<IntradayTickData>>();
        private readonly Dictionary<CorrelationID, IList<IntradayBar>> _intradayBarCache = new Dictionary<CorrelationID, IList<IntradayBar>>();

        private int _lastCorrelationId;

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

            if (serverHostname == null)
            {
                _authenticator = new PassingAuthenticator();
            }
            else
            {
#if NO_AUTHENTICATION
                authenticator = new PassingAuthenticator();
#else
                _authenticator = new Authenticator(_session, this, clientHostname, uuid);
#endif
            }

            _lastCorrelationId = 0;
        }

        #region Public Methods

        public void Start()
        {
            lock (_session)
            {
                if (!_session.Start())
                    throw new Exception("Failed to start session");

                if (!_session.OpenService("//blp/mktdata"))
                    throw new Exception("Failed to open service \"//blp/mktdata\"");
                _mktDataService = _session.GetService("//blp/mktdata");

                if (!_session.OpenService("//blp/refdata"))
                    throw new Exception("Failed to open service \"//blp/refdata\"");
                _refDataService = _session.GetService("//blp/refdata");
            }
#if NO_AUTHENTICATION
            authenticator.Authorise();
#endif
        }

        public void Stop()
        {
            lock (_session)
                _session.Stop();
        }

        public void Desubscribe(CorrelationID correlationId)
        {
            lock (_session)
                _session.Cancel(correlationId);
        }

        public void Desubscribe(IList<CorrelationID> correlators)
        {
            lock (_session)
                _session.Cancel(correlators);
        }

        public Dictionary<string, CorrelationID> Subscribe(IEnumerable<string> tickers, IList<string> fields)
        {
            if (_authenticator.AuthenticationState != AuthenticationState.Succeeded)
            {
                NotifyAuthenticationResponse(false);
                return new Dictionary<string, CorrelationID>();
            }

            lock (_session)
            {
                var subscriptions = new Dictionary<string, Subscription>();
                var realtimeRequestIds = new Dictionary<string, CorrelationID>();

                foreach (string ticker in tickers)
                {
                    //Trace.TraceInformation("Asking for ticker: {0}", ticker);

                    var correlationId = new CorrelationID(++_lastCorrelationId);
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
                _session.Subscribe(new List<Subscription>(subscriptions.Values));
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
            if (_authenticator.AuthenticationState != AuthenticationState.Succeeded)
            {
                NotifyAuthenticationResponse(false);
                return;
            }

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

        private void ProcessAuthorisationMessage(Message message)
        {
            if (message.MessageType.Equals(ElementNames.AuthorizationFailure))
                OnAuthenticationStatus(false);
            else if (message.MessageType.Equals(ElementNames.AuthorizationSuccess))
                OnAuthenticationStatus(true);
            else
                OnAuthenticationStatus(false);
        }

        private void ProcessAdminMessage(Event e)
        {
            if (!e.IsValid) return;

            foreach (var message in e.GetMessages())
            {
                switch (message.MessageType.ToString())
                {
                    case "SlowConsumerWarning":
                        NotifyAdminStatus(AdminStatus.SlowConsumerWarning);
                        break;
                    case "SlowConsumerWarningCleared":
                        NotifyAdminStatus(AdminStatus.SlowConsumerWarningCleared);
                        break;
                    default:
                        Trace.TraceWarning("unknown message type {0}", message.MessageType);
                        break;
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

            foreach (var m in e.GetMessages())
            {
                if (m.MessageType.Equals(ElementNames.AuthorizationFailure) || m.MessageType.Equals(ElementNames.AuthorizationSuccess))
                    ProcessAuthorisationMessage(m);
                if (m.MessageType.Equals(ElementNames.IntradayBarResponse))
                    ProcessIntradayBarResponse(m, isPartialResponse);
                else if (m.MessageType.Equals(ElementNames.IntradayTickResponse))
                    ProcessIntradayTickResponse(m, isPartialResponse);
                else if (m.MessageType.Equals(ElementNames.HistoricalDataResponse))
                    ProcessHistoricalDataResponse(m, isPartialResponse);
                else if (m.MessageType.Equals(ElementNames.ReferenceDataResponse))
                    ProcessReferenceDataResponse(m, isPartialResponse);
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
            OnNotifyErrorResponse(name, m.MessageType.ToString(), string.Format("RESPONSE_ERROR: {0}", m.GetElement(ElementNames.ResponseError)));
            return true;
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
                    NotifyIntradayBarReceived(ticker, intradayBars, ExtractEids(eidDataElement));
                else
                    NotifyAuthenticationError(ticker, message.MessageType.ToString(), ExtractEids(eidDataElement));
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
                    NotifyIntradayTickDataReceived(ticker, intradayTickData, ExtractEids(eidDataElement));
                else
                    NotifyAuthenticationError(ticker, m.MessageType.ToString(), ExtractEids(eidDataElement));
            }
        }

        private void ProcessHistoricalDataResponse(Message message, bool isPartialResponse)
        {
            if (message.HasElement(ElementNames.ResponseError))
            {
                Trace.TraceInformation("RESPONSE_ERROR: {0}", message.GetElement(ElementNames.ResponseError));
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
                    NotifyResponseStatus(
                        ticker,
                        ResponseStatus.InvalidSecurity,
                        securityError.GetElement(ElementNames.Source).GetValueAsString(),
                        securityError.GetElement(ElementNames.Category).GetValueAsString(),
                        securityError.GetElement(ElementNames.Code).GetValueAsInt32(),
                        securityError.GetElement(ElementNames.SubCategory).GetValueAsString(),
                        securityError.GetElement(ElementNames.Message).GetValueAsString());
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

                NotifyHistoricalDataReceived(ticker, historicalMessageWrapper);
            }
        }

        private void ProcessReferenceDataResponse(Message m, bool isPartialResponse)
        {
            if (m.HasElement(ElementNames.ResponseError))
            {
                Trace.TraceInformation("RESPONSE_ERROR: {0}", m.GetElement(ElementNames.ResponseError));
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
                    NotifyResponseStatus(
                        ticker,
                        ResponseStatus.InvalidSecurity,
                        securityError.GetElement(ElementNames.Source).GetValueAsString(),
                        securityError.GetElement(ElementNames.Category).GetValueAsString(),
                        securityError.GetElement(ElementNames.Code).GetValueAsInt32(),
                        securityError.GetElement(ElementNames.SubCategory).GetValueAsString(),
                        securityError.GetElement(ElementNames.Message).GetValueAsString());
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

                NotifyDataReceived(ticker, messageWrapper);
            }
        }

        private void ProcessSubscription(Event e)
        {
            if (e.IsValid)
            {
                foreach (var m in e.GetMessages())
                {
                    var messageWrapper = new Dictionary<string, object>();

                    foreach (var field in m.Elements)
                    {
                        if (field.IsNull)
                            continue;

                        var name = field.Name.ToString();
                        var value = field.GetFieldValue();

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
                                reason.GetElement(ElementNames.Source).GetValueAsString(),
                                reason.GetElement(ElementNames.Category).GetValueAsString(),
                                reason.GetElement(ElementNames.ErrorCode).GetValueAsInt32(),
                                reason.GetElement(ElementNames.Description).GetValueAsString());
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

                        if (status == FieldSubscriptionStatus.None)
                        {
                            Trace.TraceWarning("unknown subscription status message type: {0}", m);
                        }
                        else
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
                foreach (var m in e.GetMessages())
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
                            Trace.TraceWarning("unhandled message type {0}", m.MessageType);
                            break;
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
                            NotifySessionStatus(SessionStatus.Started);
                            break;
                        case "SessionStopped":
                            NotifySessionStatus(SessionStatus.Stopped);
                            break;
                        default:
                            Trace.TraceWarning("unknown message type {0}", m.MessageType);
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

        static class ElementNames
        {
            public static readonly Name Category = new Name("category");
            public static readonly Name Code = new Name("code");
            public static readonly Name Description = new Name("description");
            public static readonly Name ErrorInfo = new Name("errorInfo");
            public static readonly Name ErrorCode = new Name("errorCode");
            public static readonly Name Exceptions = new Name("exceptions");
            public static readonly Name FieldData = new Name("fieldData");
            public static readonly Name FieldExceptions = new Name("fieldExceptions");
            public static readonly Name FieldId = new Name("fieldId");
            public static readonly Name Message = new Name("message");
            public static readonly Name Reason = new Name("reason");
            public static readonly Name ResponseError = new Name("responseError");
            public static readonly Name Security = new Name("security");
            public static readonly Name SecurityData = new Name("securityData");
            public static readonly Name SecurityError = new Name("securityError");
            public static readonly Name Source = new Name("source");
            public static readonly Name SubCategory = new Name("subcategory");
            public static readonly Name TickData = new Name("tickData");

            public static readonly Name ReferenceDataResponse = new Name("ReferenceDataResponse");
            public static readonly Name HistoricalDataResponse = new Name("HistoricalDataResponse");
            public static readonly Name IntradayTickResponse = new Name("IntradayTickResponse");
            public static readonly Name IntradayBarResponse = new Name("IntradayBarResponse");

            public static readonly Name AuthorizationFailure = new Name("AuthorizationFailure");
            public static readonly Name AuthorizationSuccess = new Name("AuthorizationSuccess");
        }
    }
}
