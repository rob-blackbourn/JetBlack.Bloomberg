using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Managers
{
    internal abstract class Manager
    {
        protected readonly Session Session;

        protected Manager(Session session)
        {
            Session = session;
        }
    }
}
