using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// A global message broker that provides decoupled communication between components
    /// using the reactive pattern. Supports message filtering, automatic cleanup, and
    /// performance optimizations including Dictionary lookups and weak references.
    /// </summary>
    public sealed class MessageBroker : IDisposable
    {
        private static readonly Lazy<MessageBroker> _instance = new Lazy<MessageBroker>(() => new MessageBroker());
        
        /// <summary>
        /// Gets the global MessageBroker instance.
        /// </summary>
        public static MessageBroker Global => _instance.Value;

        private readonly Dictionary<Type, ISubject<object>> _subjects;
        private readonly Dictionary<Type, List<WeakReference>> _weakSubscriptions;
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessageBroker class.
        /// </summary>
        public MessageBroker()
        {
            _subjects = new Dictionary<Type, ISubject<object>>();
            _weakSubscriptions = new Dictionary<Type, List<WeakReference>>();
        }

        /// <summary>
        /// Publishes a message of the specified type to all subscribers.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to publish.</param>
        public void Publish<T>(T message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            CheckDisposed();

            var messageType = typeof(T);
            ISubject<object> subject = null;

            lock (_lock)
            {
                if (_subjects.TryGetValue(messageType, out subject))
                {
                    // Clean up dead weak references before publishing
                    CleanupWeakReferences(messageType);
                }
            }

            subject?.OnNext(message);
        }

        /// <summary>
        /// Subscribes to messages of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of messages to subscribe to.</typeparam>
        /// <param name="onNext">Action to invoke when a message is received.</param>
        /// <returns>A disposable that can be used to unsubscribe.</returns>
        public IDisposable Subscribe<T>(Action<T> onNext)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));

            return Subscribe<T>(Observer.Create<T>(onNext));
        }

        /// <summary>
        /// Subscribes to messages of the specified type with error handling.
        /// </summary>
        /// <typeparam name="T">The type of messages to subscribe to.</typeparam>
        /// <param name="onNext">Action to invoke when a message is received.</param>
        /// <param name="onError">Action to invoke when an error occurs.</param>
        /// <returns>A disposable that can be used to unsubscribe.</returns>
        public IDisposable Subscribe<T>(Action<T> onNext, Action<Exception> onError)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            return Subscribe<T>(Observer.Create<T>(onNext, onError));
        }

        /// <summary>
        /// Subscribes to messages of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of messages to subscribe to.</typeparam>
        /// <param name="observer">The observer to subscribe.</param>
        /// <returns>A disposable that can be used to unsubscribe.</returns>
        public IDisposable Subscribe<T>(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            CheckDisposed();

            var messageType = typeof(T);
            ISubject<object> subject;

            lock (_lock)
            {
                if (!_subjects.TryGetValue(messageType, out subject))
                {
                    subject = new Subject<object>();
                    _subjects[messageType] = subject;
                    _weakSubscriptions[messageType] = new List<WeakReference>();
                }
            }

            // Create a typed observer wrapper
            var typedObserver = Observer.Create<object>(
                obj =>
                {
                    if (obj is T typedMessage)
                    {
                        observer.OnNext(typedMessage);
                    }
                },
                observer.OnError,
                observer.OnCompleted
            );

            var subscription = subject.Subscribe(typedObserver);

            // Store weak reference for cleanup
            lock (_lock)
            {
                if (_weakSubscriptions.TryGetValue(messageType, out var weakRefs))
                {
                    weakRefs.Add(new WeakReference(subscription));
                }
            }

            return Disposable.Create(() =>
            {
                subscription.Dispose();
                RemoveWeakReference(messageType, subscription);
            });
        }

        /// <summary>
        /// Subscribes to messages of the specified type with filtering.
        /// </summary>
        /// <typeparam name="T">The type of messages to subscribe to.</typeparam>
        /// <param name="filter">Predicate to filter messages.</param>
        /// <param name="onNext">Action to invoke when a filtered message is received.</param>
        /// <returns>A disposable that can be used to unsubscribe.</returns>
        public IDisposable Subscribe<T>(Func<T, bool> filter, Action<T> onNext)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));

            return Subscribe<T>(message =>
            {
                if (filter(message))
                {
                    onNext(message);
                }
            });
        }

        /// <summary>
        /// Gets an observable sequence for messages of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of messages to observe.</typeparam>
        /// <returns>An observable sequence of messages.</returns>
        public IObservable<T> GetObservable<T>()
        {
            CheckDisposed();

            return Observable.Create<T>(observer =>
            {
                return Subscribe<T>(observer);
            });
        }

        /// <summary>
        /// Clears all subscriptions for the specified message type.
        /// </summary>
        /// <typeparam name="T">The type of messages to clear.</typeparam>
        public void Clear<T>()
        {
            var messageType = typeof(T);
            
            lock (_lock)
            {
                if (_subjects.TryGetValue(messageType, out var subject))
                {
                    if (subject is IDisposable disposableSubject)
                    {
                        disposableSubject.Dispose();
                    }
                    _subjects.Remove(messageType);
                }

                if (_weakSubscriptions.TryGetValue(messageType, out var weakRefs))
                {
                    weakRefs.Clear();
                    _weakSubscriptions.Remove(messageType);
                }
            }
        }

        /// <summary>
        /// Clears all subscriptions for all message types.
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                foreach (var subject in _subjects.Values)
                {
                    if (subject is IDisposable disposableSubject)
                    {
                        disposableSubject.Dispose();
                    }
                }
                _subjects.Clear();

                foreach (var weakRefs in _weakSubscriptions.Values)
                {
                    weakRefs.Clear();
                }
                _weakSubscriptions.Clear();
            }
        }

        /// <summary>
        /// Gets the number of active subscriptions for the specified message type.
        /// </summary>
        /// <typeparam name="T">The type of messages.</typeparam>
        /// <returns>The number of active subscriptions.</returns>
        public int GetSubscriptionCount<T>()
        {
            var messageType = typeof(T);
            
            lock (_lock)
            {
                if (_weakSubscriptions.TryGetValue(messageType, out var weakRefs))
                {
                    CleanupWeakReferences(messageType);
                    return weakRefs.Count;
                }
            }

            return 0;
        }

        private void CleanupWeakReferences(Type messageType)
        {
            if (_weakSubscriptions.TryGetValue(messageType, out var weakRefs))
            {
                for (int i = weakRefs.Count - 1; i >= 0; i--)
                {
                    if (!weakRefs[i].IsAlive)
                    {
                        weakRefs.RemoveAt(i);
                    }
                }
            }
        }

        private void RemoveWeakReference(Type messageType, IDisposable subscription)
        {
            lock (_lock)
            {
                if (_weakSubscriptions.TryGetValue(messageType, out var weakRefs))
                {
                    for (int i = weakRefs.Count - 1; i >= 0; i--)
                    {
                        if (!weakRefs[i].IsAlive || ReferenceEquals(weakRefs[i].Target, subscription))
                        {
                            weakRefs.RemoveAt(i);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageBroker));
        }

        /// <summary>
        /// Releases all resources used by the MessageBroker.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                ClearAll();
            }
        }
    }

    /// <summary>
    /// Provides extension methods for MessageBroker integration.
    /// </summary>
    public static class MessageBrokerExtensions
    {
        /// <summary>
        /// Publishes a message to the global MessageBroker.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to publish.</param>
        public static void Publish<T>(this T message)
        {
            MessageBroker.Global.Publish(message);
        }

        /// <summary>
        /// Subscribes to messages from the global MessageBroker and automatically
        /// disposes the subscription when the GameObject is destroyed.
        /// </summary>
        /// <typeparam name="T">The type of messages to subscribe to.</typeparam>
        /// <param name="component">The component to track for disposal.</param>
        /// <param name="onNext">Action to invoke when a message is received.</param>
        /// <returns>A disposable that can be used to unsubscribe.</returns>
        public static IDisposable SubscribeToMessage<T>(this Component component, Action<T> onNext)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            var subscription = MessageBroker.Global.Subscribe<T>(onNext);
            return subscription.AddTo(component);
        }

        /// <summary>
        /// Subscribes to filtered messages from the global MessageBroker and automatically
        /// disposes the subscription when the GameObject is destroyed.
        /// </summary>
        /// <typeparam name="T">The type of messages to subscribe to.</typeparam>
        /// <param name="component">The component to track for disposal.</param>
        /// <param name="filter">Predicate to filter messages.</param>
        /// <param name="onNext">Action to invoke when a filtered message is received.</param>
        /// <returns>A disposable that can be used to unsubscribe.</returns>
        public static IDisposable SubscribeToMessage<T>(this Component component, Func<T, bool> filter, Action<T> onNext)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            var subscription = MessageBroker.Global.Subscribe<T>(filter, onNext);
            return subscription.AddTo(component);
        }
    }
}
