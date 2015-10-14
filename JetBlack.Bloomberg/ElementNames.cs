using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg
{
    public static class ElementNames
    {
        public static readonly Name Category = new Name("category");
        public static readonly Name Code = new Name("code");
        public static readonly Name Description = new Name("description");
        public static readonly Name ErrorInfo = new Name("errorInfo");
        public static readonly Name ErrorCode = new Name("errorCode");
        public static readonly Name Exceptions = new Name("exceptions");
        public static readonly Name FieldData = new Name("fieldData");
        public static readonly Name FieldExceptions = new Name("fieldExceptions");
        public static readonly Name FieldId = new Name("fieldId");
        public static readonly Name Message = new Name("message");
        public static readonly Name Reason = new Name("reason");
        public static readonly Name ResponseError = new Name("responseError");
        public static readonly Name Security = new Name("security");
        public static readonly Name SecurityData = new Name("securityData");
        public static readonly Name SecurityError = new Name("securityError");
        public static readonly Name Source = new Name("source");
        public static readonly Name SubCategory = new Name("subcategory");
        public static readonly Name TickData = new Name("tickData");
        public static readonly Name Token = Name.GetName("token");
    }
}