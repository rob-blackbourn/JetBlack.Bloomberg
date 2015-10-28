using System.Collections.Generic;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Responses
{
    public class ReferenceDataResponse : Dictionary<string,Either<ResponseError, FieldData>>
    {
    }
}
