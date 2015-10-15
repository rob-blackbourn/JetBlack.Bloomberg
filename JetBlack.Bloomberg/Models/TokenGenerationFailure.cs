namespace JetBlack.Bloomberg.Models
{
    public class TokenGenerationFailure
    {
        public TokenGenerationFailure(string source, int errorCode, string category, string subCategory, string description)
        {
            Source = source;
            ErrorCode = errorCode;
            Category = category;
            SubCategory = subCategory;
            Description = description;
        }

        public string Source { get; private set; }
        public int ErrorCode { get; private set; }
        public string Category { get; private set; }
        public string SubCategory { get; private set; }
        public string Description { get; private set; }
    }
}
