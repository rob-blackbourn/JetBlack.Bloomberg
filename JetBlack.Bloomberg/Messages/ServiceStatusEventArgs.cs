using System;

namespace JetBlack.Bloomberg.Messages
{
    public class ServiceStatusEventArgs : EventArgs
    {
        public ServiceStatusEventArgs(string name, ServiceStatus serviceStatus)
        {
            Name = name;
            ServiceStatus = serviceStatus;
        }

        public string Name { get; private set; }
        public ServiceStatus ServiceStatus { get; private set; }
    }
}
