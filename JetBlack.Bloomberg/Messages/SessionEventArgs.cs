using System;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Messages
{
    public class SessionEventArgs<T> : EventArgs where T:EventArgs
    {
        public SessionEventArgs(Session session, T eventArgs)
        {
            Session = session;
            EventArgs = eventArgs;
        }

        public Session Session { get; private set; }
        public T EventArgs { get; private set; }
    }
}
