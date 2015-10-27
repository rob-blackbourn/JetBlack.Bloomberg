using System;
using System.Collections.Generic;
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
    internal class HistoricalDataManager : RequestResponseManager<HistoricalDataRequest, HistoricalDataResponse>, IHistoricalDataProvider
    {
        private readonly Service _service;
        private readonly Identity _identity;

        public HistoricalDataManager(Session session, Service service, Identity identity)
            : base(session)
        {
            _service = service;
            _identity = identity;
        }

        public override IObservable<HistoricalDataResponse> ToObservable(HistoricalDataRequest request)
        {
            return Observable.Create<HistoricalDataResponse>(observer =>
            {
                var correlationId = new CorrelationID();
                Observers.Add(correlationId, observer);
                Session.SendRequest(request.ToRequest(_service), _identity, correlationId);
                return Disposable.Create(() => Session.Cancel(correlationId));
            });
        }

        public override bool CanProcessResponse(Message message)
        {
            return MessageTypeNames.HistoricalDataResponse.Equals(message.MessageType);
        }

        public override void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            IObserver<HistoricalDataResponse> observer;
            if (!Observers.TryGetValue(message.CorrelationID, out observer))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                // We assume that no more messages will be sent on this correlation id.
                observer.OnError(new ContentException<ResponseError>(message.GetElement(ElementNames.ResponseError).ToResponseError()));
                Observers.Remove(message.CorrelationID);
                return;
            }

            var historicalDataResponse = new HistoricalDataResponse();

            var securityDataArrayElement = message.GetElement(ElementNames.SecurityData);

            for (var securityIndex = 0; securityIndex < securityDataArrayElement.NumValues; ++securityIndex)
            {
                var securityDataElement = securityDataArrayElement.GetElement(securityIndex);
                var ticker = securityDataElement.GetValueAsString();

                if (securityDataArrayElement.HasElement(ElementNames.SecurityError))
                {
                    var securityError = securityDataArrayElement.GetElement(ElementNames.SecurityError).ToSecurityError();
                    historicalDataResponse.Add(ticker, Either.Left<SecurityError,HistoricalData>(securityError));
                    continue;
                }

                var historicalData = new HistoricalData();

                var fieldDataArrayElement = securityDataArrayElement.GetElement(ElementNames.FieldData);

                for (var fieldDataIndex = 0; fieldDataIndex < fieldDataArrayElement.NumValues; ++fieldDataIndex)
                {
                    var data = new Dictionary<string, object>();
                    var fieldDataElement = fieldDataArrayElement.GetValueAsElement(fieldDataIndex);

                    for (var i = 0; i < fieldDataElement.NumElements; ++i)
                    {
                        var fieldElement = fieldDataElement.GetElement(i);
                        var name = fieldElement.Name.ToString();
                        var value = fieldElement.GetFieldValue();
                        if (data.ContainsKey(name))
                            data[name] = value;
                        else
                            data.Add(name, value);
                    }

                    if (data.ContainsKey("date"))
                    {
                        var date = (DateTime)data["date"];
                        data.Remove("date");
                        historicalData.Add(new KeyValuePair<DateTime, IDictionary<string, object>>(date, data));
                    }
                }

                historicalDataResponse.Add(ticker, Either.Right<SecurityError, HistoricalData>(historicalData));
            }

            observer.OnNext(historicalDataResponse);

            if (!isPartialResponse)
            {
                observer.OnCompleted();
                Observers.Remove(message.CorrelationID);
            }
        }
    }
}
