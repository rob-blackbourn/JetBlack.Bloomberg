using System;

namespace JetBlack.Bloomberg.Exceptions
{
    public class ContentException<T> : Exception
    {
        public ContentException(T content)
        {
            Content = content;
        }

        public ContentException(T content, string message)
            : base(message)
        {
            Content = content;
        }

        public ContentException(T content, string message, Exception innerException)
            : base(message, innerException)
        {
            Content = content;
        }

        public T Content { get; private set; }
    }
}
