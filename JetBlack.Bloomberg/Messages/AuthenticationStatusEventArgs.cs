using System;

namespace JetBlack.Bloomberg.Messages
{
    public class AuthenticationStatusEventArgs : EventArgs
    {
        public AuthenticationStatusEventArgs(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public bool IsSuccess { get; private set; }
    }
}
