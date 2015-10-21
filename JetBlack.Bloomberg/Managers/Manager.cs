using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Managers
{
    internal class Manager
    {
        protected readonly Session Session;

        public Manager(Session session)
        {
            Session = session;
        }
    }
}
