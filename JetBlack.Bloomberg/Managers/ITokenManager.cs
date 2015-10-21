using JetBlack.Monads;

namespace JetBlack.Bloomberg.Managers
{
    public interface ITokenManager
    {
        string GenerateToken();
        IPromise<string> RequestToken();
    }
}