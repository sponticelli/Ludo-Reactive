using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents a value that changes over time.
    /// Observers can subscribe to the subject to receive the last (or initial) value and all subsequent notifications.
    /// </summary>
    /// <typeparam name="T">The type of the elements processed by the subject.</typeparam>
    public sealed class BehaviorSubject<T> : ISubject<T>, IDisposable
    {
        private readonly object _lock = new object();
        private List<IObserver<T>> _observers;
        private bool _disposed;
        private bool _hasCompleted;
        private Exception _error;
        private T _value;

        /// <summary>
        /// Initializes a new instance of the BehaviorSubject class which creates a subject with an initial value.
        /// </summary>
        /// <param name="value">Initial value sent to observers when no other value has been received by the subject yet.</param>
        public BehaviorSubject(T value)
        {
            _observers = new List<IObserver<T>>();
            _value = value;
        }

        /// <summary>
        /// Gets the current value of the subject.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the subject has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the subject has terminated with an error or has completed.</exception>
        public T Value
        {
            get
            {
                lock (_lock)
                {
                    CheckDisposed();
                    if (_error != null)
                        throw new InvalidOperationException("Subject has terminated with an error.", _error);
                    if (_hasCompleted)
                        throw new InvalidOperationException("Subject has completed.");
                    return _value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the subject has observers.
        /// </summary>
        public bool HasObservers
        {
            get
            {
                lock (_lock)
                {
                    return _observers != null && _observers.Count > 0;
                }
            }
        }

        /// <summary>
        /// Gets the number of observers subscribed to the subject.
        /// </summary>
        public int ObserverCount
        {
            get
            {
                lock (_lock)
                {
                    return _observers?.Count ?? 0;
                }
            }
        }

        /// <summary>
        /// Notifies all subscribed observers about the arrival of the specified element in the sequence.
        /// </summary>
        /// <param name="value">The value to send to all observers.</param>
        public void OnNext(T value)
        {
            List<IObserver<T>> observers = null;

            lock (_lock)
            {
                CheckDisposed();
                if (!_hasCompleted && _error == null && _observers != null)
                {
                    _value = value;
                    observers = new List<IObserver<T>>(_observers);
                }
            }

            if (observers != null)
            {
                foreach (var observer in observers)
                {
                    try
                    {
                        observer.OnNext(value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Observer OnNext threw exception: {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Notifies all subscribed observers about the specified exception.
        /// </summary>
        /// <param name="error">The exception to send to all observers.</param>
        public void OnError(Exception error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            List<IObserver<T>> observers = null;

            lock (_lock)
            {
                CheckDisposed();
                if (!_hasCompleted && _error == null && _observers != null)
                {
                    _error = error;
                    observers = new List<IObserver<T>>(_observers);
                    _observers.Clear();
                }
            }

            if (observers != null)
            {
                foreach (var observer in observers)
                {
                    try
                    {
                        observer.OnError(error);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Observer OnError threw exception: {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Notifies all subscribed observers about the end of the sequence.
        /// </summary>
        public void OnCompleted()
        {
            List<IObserver<T>> observers = null;

            lock (_lock)
            {
                CheckDisposed();
                if (!_hasCompleted && _error == null && _observers != null)
                {
                    _hasCompleted = true;
                    observers = new List<IObserver<T>>(_observers);
                    _observers.Clear();
                }
            }

            if (observers != null)
            {
                foreach (var observer in observers)
                {
                    try
                    {
                        observer.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Observer OnCompleted threw exception: {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Subscribes an observer to the observable sequence.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            lock (_lock)
            {
                CheckDisposed();

                if (_error != null)
                {
                    observer.OnError(_error);
                    return Disposable.Empty;
                }

                if (_hasCompleted)
                {
                    observer.OnCompleted();
                    return Disposable.Empty;
                }

                // Send current value immediately
                try
                {
                    observer.OnNext(_value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Observer OnNext threw exception during subscription: {ex}");
                }

                _observers.Add(observer);
                return Disposable.Create(() => RemoveObserver(observer));
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the BehaviorSubject class.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _observers?.Clear();
                    _observers = null;
                }
            }
        }

        private void RemoveObserver(IObserver<T> observer)
        {
            lock (_lock)
            {
                if (!_disposed && _observers != null)
                {
                    _observers.Remove(observer);
                }
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BehaviorSubject<T>));
        }
    }
}
