using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Defines a provider for push-based notification.
    /// This is the core abstraction representing a stream of values over time.
    /// </summary>
    /// <typeparam name="T">The object that provides notification information.</typeparam>
    public interface IObservable<out T>
    {
        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving 
        /// notifications before the provider has finished sending them.</returns>
        IDisposable Subscribe(IObserver<T> observer);
    }
}
