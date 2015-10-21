using JetBlack.Monads;

namespace JetBlack.Bloomberg
{
    public interface ITokenProvider
    {
        string GenerateToken();
        IPromise<string> RequestToken();
    }
}