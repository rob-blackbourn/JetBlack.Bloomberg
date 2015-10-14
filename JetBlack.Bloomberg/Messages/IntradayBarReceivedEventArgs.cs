using System;
using System.Collections.Generic;

namespace JetBlack.Bloomberg.Messages
{
    public class IntradayBarReceivedEventArgs : EventArgs
    {
        public IntradayBarReceivedEventArgs(string name, IList<IntradayBar> intradayTickDataList, IEnumerable<int> eids)
        {
            Name = name;
            IntradayTickDataList = intradayTickDataList;
            Eids = eids;
        }

        public string Name { get; private set; }
        public IList<IntradayBar> IntradayTickDataList { get; private set; }
        public IEnumerable<int> Eids { get; private set; }
    }
}
