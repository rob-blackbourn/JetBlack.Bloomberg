using System;

namespace JetBlack.Bloomberg.Messages
{
    public class ResponseStatusEventArgs : EventArgs
    {
        public ResponseStatusEventArgs(string name, ResponseStatus responseStatus, string source, string category, int code, string subCategory, string message)
        {
            Name = name;
            ResponseStatus = responseStatus;
            Source = source;
            Category = category;
            Code = code;
            SubCategory = subCategory;
            Message = message;
        }

        public string Name { get; private set; }
        public ResponseStatus ResponseStatus { get; private set; }
        public string Source { get; private set; }
        public string Category { get; private set; }
        public int Code { get; private set; }
        public string SubCategory { get; private set; }
        public string Message { get; private set; }
    }
}
