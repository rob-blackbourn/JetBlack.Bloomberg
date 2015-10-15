namespace JetBlack.Bloomberg.Messages
{
    public class TokenGenerationSuccess
    {
        public TokenGenerationSuccess(string token)
        {
            Token = token;
        }

        public string Token { get; private set; }
    }
}
