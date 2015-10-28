using System.Collections.Generic;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Responses
{
    public class SubscriptionFailure
    {
        public SubscriptionFailure(ResponseError error)
            : this(Either.Left<ResponseError, IDictionary<string, ResponseError>>(error))
        {
        }

        public SubscriptionFailure(IDictionary<string, ResponseError> error)
            : this(Either.Right<ResponseError, IDictionary<string, ResponseError>>(error))
        {
        }

        public SubscriptionFailure(Either<ResponseError, IDictionary<string, ResponseError>> error)
        {
            Error = error;
        }

        public Either<ResponseError, IDictionary<string, ResponseError>> Error { get; private set; }
    }
}
