using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Models;
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
                // We assume no more messages will be delivered for this correlation id.
                observer.OnError(new ContentException<ResponseError>(message.GetElement(ElementNames.ResponseError).ToResponseError()));
                Observers.Remove(message.CorrelationID);
                return;
            }

            var referenceDataResponse = new ReferenceDataResponse();

            var securities = message.GetElement(ElementNames.SecurityData);
            for (var i = 0; i < securities.NumValues; ++i)
            {
                var security = securities.GetValueAsElement(i);
                var ticker = security.GetElementAsString(ElementNames.Security);
                
                if (security.HasElement(ElementNames.SecurityError))
                {
                    referenceDataResponse.Add(ticker, Either.Left<SecurityError,FieldData>(security.GetElement(ElementNames.SecurityError).ToSecurityError()));
                    continue;
                }

                var fieldData = new FieldData();

                var fields = security.GetElement(ElementNames.FieldData);
                for (var j = 0; j < fields.NumElements; ++j)
                {
                    var field = fields.GetElement(j);
                    var name = field.Name.ToString();
                    var value = field.GetFieldValue();
                    if (fieldData.ContainsKey(name))
                        fieldData[name] = value;
                    else
                        fieldData.Add(name, value);
                }

                referenceDataResponse.Add(ticker, Either.Right<SecurityError,FieldData>(fieldData));
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
