using System;

namespace JetBlack.Bloomberg.Messages
{
    public class EventArgsException<T> : Exception where T:EventArgs
    {
        public EventArgsException(string message, T eventArgs)
            : base(message)
        {
            EventArgs = eventArgs;
        }

        public EventArgsException(string message, T eventArgs, Exception innerException)
            : base(message, innerException)
        {
            EventArgs = eventArgs;
        }

        public EventArgsException(T eventArgs)
        {
            EventArgs = eventArgs;
        }

        public T EventArgs { get; private set; }
    }
}
