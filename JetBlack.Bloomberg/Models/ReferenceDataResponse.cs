using System.Collections.Generic;
using JetBlack.Monads;

namespace JetBlack.Bloomberg.Models
{
    public class ReferenceDataResponse : Dictionary<string,Either<SecurityError,FieldData>>
    {
    }
}
