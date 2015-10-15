using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg.Models
{
    public class SessionDecorator<T>
    {
        public SessionDecorator(Session session, T content)
        {
            Content = content;
            Session = session;
        }

        public Session Session { get; private set; }
        public T Content { get; private set; }
    }
}
