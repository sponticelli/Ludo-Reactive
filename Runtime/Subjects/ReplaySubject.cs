using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents an object that is both an observable sequence as well as an observer.
    /// Each notification is broadcasted to all subscribed and future observers, subject to buffer trimming policies.
    /// </summary>
    /// <typeparam name="T">The type of the elements processed by the subject.</typeparam>
    public sealed class ReplaySubject<T> : ISubject<T>, IDisposable
    {
        private readonly object _lock = new object();
        private List<IObserver<T>> _observers;
        private Queue<T> _buffer;
        private bool _disposed;
        private bool _hasCompleted;
        private Exception _error;
        private readonly int _bufferSize;
        private readonly TimeSpan _window;
        private readonly Queue<DateTime> _timestamps;

        /// <summary>
        /// Initializes a new instance of the ReplaySubject class with the specified buffer size.
        /// </summary>
        /// <param name="bufferSize">Maximum element count of the replay buffer.</param>
        public ReplaySubject(int bufferSize)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _observers = new List<IObserver<T>>();
            _buffer = new Queue<T>();
            _bufferSize = bufferSize;
            _window = TimeSpan.MaxValue;
            _timestamps = new Queue<DateTime>();
        }

        /// <summary>
        /// Initializes a new instance of the ReplaySubject class with the specified buffer size and window.
        /// </summary>
        /// <param name="bufferSize">Maximum element count of the replay buffer.</param>
        /// <param name="window">Maximum time length of the replay buffer.</param>
        public ReplaySubject(int bufferSize, TimeSpan window)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (window < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(window));

            _observers = new List<IObserver<T>>();
            _buffer = new Queue<T>();
            _bufferSize = bufferSize;
            _window = window;
            _timestamps = new Queue<DateTime>();
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
                    var now = DateTime.UtcNow;
                    
                    // Add to buffer
                    _buffer.Enqueue(value);
                    _timestamps.Enqueue(now);

                    // Trim buffer by size
                    while (_buffer.Count > _bufferSize)
                    {
                        _buffer.Dequeue();
                        _timestamps.Dequeue();
                    }

                    // Trim buffer by time window (only if window is not MaxValue)
                    if (_window != TimeSpan.MaxValue)
                    {
                        var cutoff = now - _window;
                        while (_timestamps.Count > 0 && _timestamps.Peek() < cutoff)
                        {
                            _buffer.Dequeue();
                            _timestamps.Dequeue();
                        }
                    }

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
                    // Replay buffered values
                    foreach (var value in _buffer)
                    {
                        try
                        {
                            observer.OnNext(value);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[Ludo.Reactive] Observer OnNext threw exception during replay: {ex}");
                        }
                    }
                    observer.OnCompleted();
                    return Disposable.Empty;
                }

                // Replay buffered values
                foreach (var value in _buffer)
                {
                    try
                    {
                        observer.OnNext(value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Observer OnNext threw exception during replay: {ex}");
                    }
                }

                _observers.Add(observer);
                return Disposable.Create(() => RemoveObserver(observer));
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the ReplaySubject class.
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
                    _buffer?.Clear();
                    _buffer = null;
                    _timestamps?.Clear();
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
                throw new ObjectDisposedException(nameof(ReplaySubject<T>));
        }
    }
}
