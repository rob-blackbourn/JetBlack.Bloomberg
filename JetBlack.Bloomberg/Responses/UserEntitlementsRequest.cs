namespace JetBlack.Bloomberg.Responses
{
    public class UserEntitlementsRequest
    {
        public UserEntitlementsRequest(int uuid)
        {
            Uuid = uuid;
        }

        public int Uuid { get; private set; }
    }
}
