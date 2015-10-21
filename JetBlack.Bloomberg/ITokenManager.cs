using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface ITokenManager
    {
        string GenerateToken();
        IPromise<string> RequestToken();
    }
}