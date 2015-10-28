using System.Collections.Generic;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Models
{
    public class SubscriptionResponse
    {
        public SubscriptionResponse(string ticker, SubscriptionFailure subscriptionFailure)
            : this(ticker, Either.Left<SubscriptionFailure, IDictionary<string, object>>(subscriptionFailure))
        {
        }

        public SubscriptionResponse(string ticker, IDictionary<string, object> data)
            : this(ticker, Either.Right<SubscriptionFailure, IDictionary<string, object>>(data))
        {
        }

        public SubscriptionResponse(string ticker, Either<SubscriptionFailure,IDictionary<string, object>> data)
        {
            Ticker = ticker;
            Data = data;
        }

        public string Ticker { get; private set; }
        public Either<SubscriptionFailure, IDictionary<string, object>> Data { get; private set; }
    }
}
