using System;

namespace JetBlack.Bloomberg
{
    public static class AsyncPattern
    {
        public static AsyncPattern<TSuccess, TFailure> Create<TSuccess, TFailure>(Action<TSuccess> onSuccess, Action<TFailure> onFailure)
        {
            return new AsyncPattern<TSuccess, TFailure>(onSuccess, onFailure);
        }
    }

    public class AsyncPattern<TSuccess, TFailure>
    {
        public AsyncPattern(Action<TSuccess> onSuccess, Action<TFailure> onFailure)
        {
            OnFailure = onFailure;
            OnSuccess = onSuccess;
        }

        public Action<TSuccess> OnSuccess { get; private set; }
        public Action<TFailure> OnFailure { get; private set; }
    }
}