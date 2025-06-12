using System;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents a connectable observable sequence that shares a single subscription to the underlying sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    internal sealed class ConnectableObservable<T> : IConnectableObservable<T>, IDisposable
    {
        private readonly IObservable<T> _source;
        private readonly Func<ISubject<T>> _subjectFactory;
        private readonly object _lock = new object();
        
        private ISubject<T> _subject;
        private IDisposable _connection;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ConnectableObservable class.
        /// </summary>
        /// <param name="source">The source observable sequence.</param>
        /// <param name="subjectFactory">Factory function to create the subject used for multicasting.</param>
        public ConnectableObservable(IObservable<T> source, Func<ISubject<T>> subjectFactory)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _subjectFactory = subjectFactory ?? throw new ArgumentNullException(nameof(subjectFactory));
        }

        /// <summary>
        /// Subscribes an observer to the connectable observable sequence.
        /// </summary>
        /// <param name="observer">The observer to subscribe.</param>
        /// <returns>A disposable that can be used to unsubscribe the observer.</returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            lock (_lock)
            {
                CheckDisposed();
                
                if (_subject == null)
                {
                    _subject = _subjectFactory();
                }
                
                return _subject.Subscribe(observer);
            }
        }

        /// <summary>
        /// Connects the observable wrapper to its source.
        /// </summary>
        /// <returns>A disposable used to disconnect the observable wrapper from its source.</returns>
        public IDisposable Connect()
        {
            lock (_lock)
            {
                CheckDisposed();
                
                if (_connection != null)
                {
                    return _connection;
                }
                
                if (_subject == null)
                {
                    _subject = _subjectFactory();
                }
                
                _connection = _source.Subscribe(_subject);
                
                return Disposable.Create(() =>
                {
                    lock (_lock)
                    {
                        if (_connection != null)
                        {
                            _connection.Dispose();
                            _connection = null;
                        }
                    }
                });
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConnectableObservable<T>));
        }

        /// <summary>
        /// Releases all resources used by the ConnectableObservable.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;
                
                _disposed = true;
                
                try
                {
                    _connection?.Dispose();
                    
                    if (_subject is IDisposable disposableSubject)
                    {
                        disposableSubject.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Exception while disposing ConnectableObservable: {ex}");
                }
                finally
                {
                    _connection = null;
                    _subject = null;
                }
            }
        }
    }

    /// <summary>
    /// Represents an object that is both an observable sequence as well as an observer.
    /// </summary>
    /// <typeparam name="T">The type of the elements processed by the subject.</typeparam>
    public interface ISubject<T> : IObservable<T>, IObserver<T>
    {
    }
}
