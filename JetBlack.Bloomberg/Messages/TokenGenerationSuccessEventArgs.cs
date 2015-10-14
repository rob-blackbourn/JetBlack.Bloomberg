using System;

namespace JetBlack.Bloomberg.Messages
{
    public class TokenGenerationSuccessEventArgs : EventArgs
    {
        public TokenGenerationSuccessEventArgs(string token)
        {
            Token = token;
        }

        public string Token { get; private set; }
    }
}
