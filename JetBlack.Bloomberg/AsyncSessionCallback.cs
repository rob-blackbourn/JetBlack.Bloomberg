using System;
using Bloomberglp.Blpapi;

namespace JetBlack.Bloomberg
{
    public static class AsyncSessionCallback
    {
        public static AsyncSessionCallback<TSuccess, TFailure> Create<TSuccess, TFailure>(Action<Session,TSuccess> onSuccess, Action<Session,TFailure> onFailure)
        {
            return new AsyncSessionCallback<TSuccess, TFailure>(onSuccess, onFailure);
        }
    }

    public class AsyncSessionCallback<TSuccess, TFailure>
    {
        public AsyncSessionCallback(Action<Session,TSuccess> onSuccess, Action<Session,TFailure> onFailure)
        {
            OnFailure = onFailure;
            OnSuccess = onSuccess;
        }

        public Action<Session,TSuccess> OnSuccess { get; private set; }
        public Action<Session,TFailure> OnFailure { get; private set; }
    }
}