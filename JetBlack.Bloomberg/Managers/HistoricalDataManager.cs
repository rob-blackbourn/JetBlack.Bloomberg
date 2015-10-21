using System;
using System.Collections.Generic;
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
    internal class HistoricalDataManager : RequestResponseManager<HistoricalDataRequest, HistoricalDataResponse>, IHistoricalDataProvider
    {
        private readonly Service _service;
        private readonly Identity _identity;

        private readonly IDictionary<CorrelationID, HistoricalDataResponse> _partial = new Dictionary<CorrelationID, HistoricalDataResponse>();

        public HistoricalDataManager(Session session, Service service, Identity identity)
            : base(session)
        {
            _service = service;
            _identity = identity;
        }

        public override IPromise<HistoricalDataResponse> Request(HistoricalDataRequest request)
        {
            return new Promise<HistoricalDataResponse>((resolve, reject) =>
            {
                var correlationId = new CorrelationID();
                AsyncHandlers.Add(correlationId, AsyncPattern<HistoricalDataResponse>.Create(resolve, reject));
                Session.SendRequest(request.ToRequest(_service), _identity, correlationId);
            });
        }

        public override void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            AsyncPattern<HistoricalDataResponse> asyncHandler;
            if (!AsyncHandlers.TryGetValue(message.CorrelationID, out asyncHandler))
            {
                onFailure(session, message, new Exception("Unable to find handler for correlation id: " + message.CorrelationID));
                return;
            }

            if (message.HasElement(ElementNames.ResponseError))
            {
                asyncHandler.OnFailure(new ContentException<ResponseError>(message.GetElement(ElementNames.ResponseError).ToResponseError()));
                return;
            }

            HistoricalDataResponse historicalDataResponse;
            if (_partial.TryGetValue(message.CorrelationID, out historicalDataResponse))
                _partial.Remove(message.CorrelationID);
            else
                historicalDataResponse = new HistoricalDataResponse(new Dictionary<string,HistoricalTickerData>());

            var securityDataArray = message.GetElement(ElementNames.SecurityData);

            for (var i = 0; i < securityDataArray.NumValues; ++i)
            {
                var securityData = securityDataArray.GetElement(i);
                var ticker = securityData.GetValueAsString();

                if (securityDataArray.HasElement(ElementNames.SecurityError))
                {
                    asyncHandler.OnFailure(new ContentException<TickerSecurityError>(new TickerSecurityError(ticker, securityDataArray.GetElement(ElementNames.SecurityError).ToSecurityError(), isPartialResponse)));
                    continue;
                }

                HistoricalTickerData historicalTickerData;
                if (historicalDataResponse.HistoricalTickerData.TryGetValue(ticker, out historicalTickerData))
                    historicalDataResponse.HistoricalTickerData.Remove(ticker);
                else
                    historicalTickerData = new HistoricalTickerData(ticker, new List<KeyValuePair<DateTime, IDictionary<string, object>>>());

                var fieldDataArray = securityDataArray.GetElement(ElementNames.FieldData);

                for (var j = 0; j < fieldDataArray.NumValues; ++j)
                {
                    var data = new Dictionary<string, object>();
                    var fieldData = fieldDataArray.GetValueAsElement(j);

                    for (var k = 0; k < fieldData.NumElements; ++k)
                    {
                        var field = fieldData.GetElement(k);
                        var name = field.Name.ToString();
                        var value = field.GetFieldValue();
                        if (data.ContainsKey(name))
                            data[name] = value;
                        else
                            data.Add(name, value);
                    }

                    if (data.ContainsKey("date"))
                    {
                        var date = (DateTime)data["date"];
                        data.Remove("date");
                        historicalTickerData.Data.Add(new KeyValuePair<DateTime, IDictionary<string, object>>(date, data));
                    }
                }

                historicalDataResponse.HistoricalTickerData[ticker] = historicalTickerData;
            }

            if (isPartialResponse)
                _partial[message.CorrelationID] = historicalDataResponse;
            else
                asyncHandler.OnSuccess(historicalDataResponse);
        }
    }
}
