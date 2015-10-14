using System;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Messages
{
    public class ServiceOpenedEventArgs : EventArgs
    {
        public ServiceOpenedEventArgs(Service service)
        {
            Service = service;
        }

        public Service Service { get; private set; }
    }
}
