using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Base interface for observables that can be subscribed to
    /// </summary>
    public interface IObservable
    {
        SubscriptionHandle Subscribe(Action callback);
        void Unsubscribe(Action callback);
    }
}