using System;

namespace JetBlack.Bloomberg.Messages
{
    public class AdminStatusEventArgs : EventArgs
    {
        public AdminStatusEventArgs(AdminStatus adminStatus)
        {
            AdminStatus = adminStatus;
        }

        public AdminStatus AdminStatus { get; private set; }
    }
}
