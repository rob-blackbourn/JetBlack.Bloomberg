using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Exceptions;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Requests;
using JetBlack.Bloomberg.Responses;
using JetBlack.Bloomberg.Utilities;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    internal class HistoricalDataManager : RequestResponseManager<HistoricalDataRequest, HistoricalDataResponse, object>, IHistoricalDataProvider
    {
        public HistoricalDataManager(Session session, Service service, Identity identity)
            : base(session, service, identity)
        {
        }

        public override IObservable<HistoricalDataResponse> ToObservable(HistoricalDataRequest request)
        {
            return Observable.Create<HistoricalDataResponse>(observer =>
            {
                var correlationId = new CorrelationID();
                Add(correlationId, observer);
                Session.SendRequest(request.ToRequest(Service), Identity, correlationId);
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
            if (!TryGet(message.CorrelationID, out observer))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                var responseErrorElement = message.GetElement(ElementNames.ResponseError);
                var error = new ResponseError(
                    responseErrorElement.GetElementAsString(ElementNames.Source),
                    responseErrorElement.GetElementAsString(ElementNames.Category),
                    responseErrorElement.GetElementAsString(ElementNames.SubCategory),
                    responseErrorElement.GetElementAsInt32(ElementNames.Code),
                    responseErrorElement.GetElementAsString(ElementNames.Message));
                observer.OnError(new ContentException<ResponseError>(error));
                // We assume that no more messages will be sent on this correlation id.
                Remove(message.CorrelationID);
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
                    var securityErrorElement = securityDataArrayElement.GetElement(ElementNames.SecurityError);
                    var responseError = new ResponseError(
                        securityErrorElement.GetElementAsString(ElementNames.Source),
                        securityErrorElement.GetElementAsString(ElementNames.Category),
                        securityErrorElement.GetElementAsString(ElementNames.SubCategory),
                        securityErrorElement.GetElementAsInt32(ElementNames.Code),
                        securityErrorElement.GetElementAsString(ElementNames.Message));

                    historicalDataResponse.Add(ticker, Either.Left<ResponseError,HistoricalData>(responseError));
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

                historicalDataResponse.Add(ticker, Either.Right<ResponseError, HistoricalData>(historicalData));
            }

            observer.OnNext(historicalDataResponse);

            if (!isPartialResponse)
            {
                observer.OnCompleted();
                Remove(message.CorrelationID);
            }
        }
    }
}
