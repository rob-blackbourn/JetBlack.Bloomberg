namespace JetBlack.Bloomberg.Models
{
    public class ResponseError
    {
        public ResponseError(string source, string category, string subCategory, int code, string message)
        {
            Source = source;
            Category = category;
            SubCategory = subCategory;
            Code = code;
            Message = message;
        }

        public string Source { get; private set; }
        public string Category { get; private set; }
        public int Code { get; private set; }
        public string SubCategory { get; private set; }
        public string Message { get; private set; }    
    }
}
