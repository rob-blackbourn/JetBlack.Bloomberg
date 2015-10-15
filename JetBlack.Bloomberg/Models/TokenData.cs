namespace JetBlack.Bloomberg.Models
{
    public class TokenData
    {
        public TokenData(string token)
        {
            Token = token;
        }

        public string Token { get; private set; }
    }
}
