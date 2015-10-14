using System;

namespace JetBlack.Bloomberg.Messages
{
    public class SessionStatusEventArgs : EventArgs
    {
        public SessionStatusEventArgs(SessionStatus sessionStatus)
        {
            SessionStatus = sessionStatus;
        }

        public SessionStatus SessionStatus { get; private set; }
    }
}
