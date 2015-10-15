using System;

namespace JetBlack.Bloomberg.Patterns
{
    public class AsyncPattern<TSuccess>
    {
        public AsyncPattern(Action<TSuccess> onSuccess, Action<Exception> onFailure)
        {
            OnFailure = onFailure;
            OnSuccess = onSuccess;
        }

        public Action<TSuccess> OnSuccess { get; private set; }
        public Action<Exception> OnFailure { get; private set; }

        public static AsyncPattern<TSuccess> Create(Action<TSuccess> onSuccess, Action<Exception> onFailure)
        {
            return new AsyncPattern<TSuccess>(onSuccess, onFailure);
        }
    }
}