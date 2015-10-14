using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Messages
{
    public class DataReceivedEventArgs : EventArgs
    {
        public DataReceivedEventArgs(string name, IDictionary<string, object> referenceDataMessage)
        {
            Name = name;
            ReferenceDataMessage = referenceDataMessage;
        }

        public string Name { get; private set; }
        public IDictionary<string, object> ReferenceDataMessage { get; private set; }
    }
}
