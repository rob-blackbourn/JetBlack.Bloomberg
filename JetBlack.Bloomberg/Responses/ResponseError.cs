namespace JetBlack.Bloomberg.Responses
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
        public string SubCategory { get; private set; }
        public int Code { get; private set; }
        public string Message { get; private set; }

        public override string ToString()
        {
            return string.Format("Source=\"{0}\", Category=\"{1}\", SubCategory=\"{2}\", Code={3}, Message=\"{4}\"", Source, Category, SubCategory, Code, Message);
        }
    }
}
