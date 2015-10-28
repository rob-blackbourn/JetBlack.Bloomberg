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

namespace JetBlack.Bloomberg.Managers
{
    internal class IntradayTickManager : RequestResponseManager<IntradayTickRequest, IntradayTickResponse, string>, IIntradayTickProvider
    {
        public IntradayTickManager(Session session, Service service, Identity identity)
            : base(session, service, identity)
        {
        }

        public override IObservable<IntradayTickResponse> ToObservable(IntradayTickRequest request)
        {
            return Observable.Create<IntradayTickResponse>(observer =>
            {
                var correlationId = new CorrelationID();
                Add(correlationId, observer, request.Ticker);
                Session.SendRequest(request.ToRequest(Service), Identity, correlationId);

                return Disposable.Create(() => Session.Cancel(correlationId));
            });
        }

        public override bool CanProcessResponse(Message message)
        {
            return MessageTypeNames.IntradayTickResponse.Equals(message.MessageType);
        }

        public override void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            IObserver<IntradayTickResponse> observer;
            string ticker;
            if (!TryGet(message.CorrelationID, out observer, out ticker))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                var responseErrorElement = message.GetElement(ElementNames.ResponseError);
                var responseError = new ResponseError(
                    responseErrorElement.GetElementAsString(ElementNames.Source),
                    responseErrorElement.GetElementAsString(ElementNames.Category),
                    responseErrorElement.GetElementAsString(ElementNames.SubCategory),
                    responseErrorElement.GetElementAsInt32(ElementNames.Code),
                    responseErrorElement.GetElementAsString(ElementNames.Message));
                observer.OnError(new ContentException<TickerResponseError>(new TickerResponseError(ticker, responseError)));

                // We assume no more messages will be delivered for this correlation id.
                Remove(message.CorrelationID);

                return;
            }

            var tickData = message.GetElement("tickData").GetElement("tickData");

            var entitlementIds = tickData.HasElement("eidData") ? tickData.GetElement("eidData").ExtractEids() : null;

            var intradayTicks = new List<IntradayTick>();
            for (var i = 0; i < tickData.NumValues; ++i)
            {
                var item = tickData.GetValueAsElement(i);
                var conditionCodes = (item.HasElement("conditionCodes") ? item.GetElementAsString("conditionCodes").Split(',') : null);
                var exchangeCodes = (item.HasElement("exchangeCode") ? item.GetElementAsString("exchangeCode").Split(',') : null);

                intradayTicks.Add(
                    new IntradayTick(
                        item.GetElementAsDatetime("time").ToDateTime(),
                        (EventType)Enum.Parse(typeof(EventType), item.GetElementAsString("type"), true),
                        item.GetElementAsFloat64("value"),
                        item.GetElementAsInt32("size"),
                        conditionCodes,
                        exchangeCodes));
            }

            var intradayTickResponse = new IntradayTickResponse(ticker, intradayTicks, entitlementIds);

            observer.OnNext(intradayTickResponse);

            if (!isPartialResponse)
            {
                observer.OnCompleted();
                Remove(message.CorrelationID);
            }
        }
    }
}
