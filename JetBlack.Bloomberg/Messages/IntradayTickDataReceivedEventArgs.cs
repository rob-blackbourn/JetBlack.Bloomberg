using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Messages
{
    public class IntradayTickDataReceivedEventArgs : EventArgs
    {
        public IntradayTickDataReceivedEventArgs(string name, IList<IntradayTickData> intradayBarList, IList<int> entitlementIds)
        {
            Name = name;
            IntradayBarList = IntradayBarList;
            EntitlementIds = entitlementIds;
        }

        public string Name { get; private set; }
        public IList<IntradayTickData> IntradayBarList { get; private set; }
        public IList<int> EntitlementIds { get; private set; }
    }
}
