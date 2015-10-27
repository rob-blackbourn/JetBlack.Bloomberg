using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Patterns;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Utilities;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    internal class ReferenceDataManager : RequestResponseManager<ReferenceDataRequest, ReferenceDataResponse>, IReferenceDataProvider
    {
        private readonly Service _service;
        private readonly Identity _identity;

        public ReferenceDataManager(Session session, Service service, Identity identity)
            : base(session)
        {
            _service = service;
            _identity = identity;
        }

        public override IObservable<ReferenceDataResponse> ToObservable(ReferenceDataRequest request)
        {
            return Observable.Create<ReferenceDataResponse>(observer =>
            {
                var correlationId = new CorrelationID();
                Observers.Add(correlationId, observer);
                Session.SendRequest(request.ToRequest(_service), _identity, correlationId);
                return Disposable.Create(() => Session.Cancel(correlationId));
            });
        }

        public override bool CanProcessResponse(Message message)
        {
            return MessageTypeNames.ReferenceDataResponse.Equals(message.MessageType);
        }

        public override void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            IObserver<ReferenceDataResponse> observer;
            if (!Observers.TryGetValue(message.CorrelationID, out observer))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                observer.OnError(new ContentException<ResponseError>(message.GetElement(ElementNames.ResponseError).ToResponseError()));
                return;
            }

            var referenceDataResponse = new ReferenceDataResponse(new Dictionary<string, TickerData>(new Dictionary<string, TickerData>()));

            var securities = message.GetElement(ElementNames.SecurityData);
            for (var i = 0; i < securities.NumValues; ++i)
            {
                var security = securities.GetValueAsElement(i);
                var ticker = security.GetElementAsString(ElementNames.Security);
                
                if (security.HasElement(ElementNames.SecurityError))
                {
                    observer.OnError(new ContentException<SecurityError>(security.GetElement(ElementNames.SecurityError).ToSecurityError()));
                    continue;
                }

                TickerData tickerData;
                if (referenceDataResponse.ReferenceData.TryGetValue(ticker, out tickerData))
                    referenceDataResponse.ReferenceData.Remove(ticker);
                else
                    tickerData = new TickerData(ticker, new Dictionary<string, object>());

                var fields = security.GetElement(ElementNames.FieldData);
                for (var j = 0; j < fields.NumElements; ++j)
                {
                    var field = fields.GetElement(j);
                    var name = field.Name.ToString();
                    var value = field.GetFieldValue();
                    if (tickerData.Data.ContainsKey(name))
                        tickerData.Data[name] = value;
                    else
                        tickerData.Data.Add(name, value);
                }

                referenceDataResponse.ReferenceData[ticker] = tickerData;
            }

            observer.OnNext(referenceDataResponse);

            if (!isPartialResponse)
            {
                observer.OnCompleted();
                Observers.Remove(message.CorrelationID);
            }
        }
    }
}
