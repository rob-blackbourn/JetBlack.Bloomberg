using System;

namespace JetBlack.Bloomberg.Messages
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T args)
        {
            Args = args;
        }

        public T Args { get; private set; }
    }
}
