using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Generic observable interface with typed notifications
    /// </summary>
    public interface IObservable<T> : IObservable
    {
        SubscriptionHandle Subscribe(Action<T> callback);
        void Unsubscribe(Action<T> callback);
    }

}