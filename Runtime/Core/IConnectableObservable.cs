using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents an observable wrapper that can be connected and disconnected from its underlying observable sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    public interface IConnectableObservable<out T> : IObservable<T>
    {
        /// <summary>
        /// Connects the observable wrapper to its source. All subscribed observers will receive values from the underlying observable sequence as long as the connection is established.
        /// </summary>
        /// <returns>Disposable used to disconnect the observable wrapper from its source, causing subscribed observer to stop receiving values from the underlying observable sequence.</returns>
        IDisposable Connect();
    }
}
