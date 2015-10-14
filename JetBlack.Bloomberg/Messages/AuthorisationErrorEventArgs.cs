using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Messages
{
    public class AuthorisationErrorEventArgs : EventArgs
    {
        public AuthorisationErrorEventArgs(string ticker, string messageType, IList<int> eids)
        {
            Ticker = ticker;
            MessageType = messageType;
            Eids = eids;
        }

        public string Ticker { get; private set; }
        public string MessageType { get; private set; }
        public IList<int> Eids { get; private set; }
    }
}
