using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents a property that can be observed for changes.
    /// Combines the Observable pattern with property semantics familiar to Unity developers.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    [Serializable]
    public class ReactiveProperty<T> : IObservable<T>, IDisposable
    {
        [SerializeField] private T _value;
        private readonly Subject<T> _subject;
        private readonly IEqualityComparer<T> _comparer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ReactiveProperty class with a default value.
        /// </summary>
        public ReactiveProperty() : this(default(T))
        {
        }

        /// <summary>
        /// Initializes a new instance of the ReactiveProperty class with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial value of the property.</param>
        public ReactiveProperty(T initialValue) : this(initialValue, EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ReactiveProperty class with the specified initial value and equality comparer.
        /// </summary>
        /// <param name="initialValue">The initial value of the property.</param>
        /// <param name="comparer">The equality comparer to use when comparing values.</param>
        public ReactiveProperty(T initialValue, IEqualityComparer<T> comparer)
        {
            _value = initialValue;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _subject = new Subject<T>();
        }

        /// <summary>
        /// Gets or sets the current value of the property.
        /// Setting the value will notify all observers if the new value is different from the current value.
        /// </summary>
        public T Value
        {
            get
            {
                CheckDisposed();
                return _value;
            }
            set
            {
                CheckDisposed();
                if (!_comparer.Equals(_value, value))
                {
                    _value = value;
                    _subject.OnNext(value);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the property has observers.
        /// </summary>
        public bool HasObservers => _subject?.HasObservers ?? false;

        /// <summary>
        /// Gets the number of observers subscribed to the property.
        /// </summary>
        public int ObserverCount => _subject?.ObserverCount ?? 0;

        /// <summary>
        /// Forces the property to notify all observers with the current value, regardless of whether it has changed.
        /// </summary>
        public void ForceNotify()
        {
            CheckDisposed();
            _subject.OnNext(_value);
        }

        /// <summary>
        /// Sets the value without triggering notifications.
        /// This is useful for initialization scenarios where you want to set the value but not notify observers.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetValueWithoutNotify(T value)
        {
            CheckDisposed();
            _value = value;
        }

        /// <summary>
        /// Subscribes an observer to the observable sequence.
        /// The observer will immediately receive the current value, then any subsequent changes.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            CheckDisposed();

            // Send current value immediately
            try
            {
                observer.OnNext(_value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Observer OnNext threw exception during subscription: {ex}");
            }

            // Subscribe to future changes
            return _subject.Subscribe(observer);
        }

        /// <summary>
        /// Subscribes to the observable sequence with the specified action.
        /// The action will immediately be called with the current value, then with any subsequent changes.
        /// </summary>
        /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
        public IDisposable Subscribe(Action<T> onNext)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));

            return Subscribe(Observer.Create(onNext));
        }





        /// <summary>
        /// Releases all resources used by the current instance of the ReactiveProperty class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _subject?.Dispose();
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ReactiveProperty<T>));
        }

        /// <summary>
        /// Implicitly converts a ReactiveProperty to its current value.
        /// </summary>
        /// <param name="property">The ReactiveProperty to convert.</param>
        /// <returns>The current value of the property.</returns>
        public static implicit operator T(ReactiveProperty<T> property)
        {
            return property != null ? property.Value : default(T);
        }

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        /// <returns>A string representation of the current value.</returns>
        public override string ToString()
        {
            return _value?.ToString() ?? "null";
        }
    }

    /// <summary>
    /// Provides a set of static methods for creating observers.
    /// </summary>
    public static class Observer
    {
        /// <summary>
        /// Creates an observer from the specified OnNext action.
        /// </summary>
        /// <typeparam name="T">The type of the elements received by the observer.</typeparam>
        /// <param name="onNext">Observer's OnNext action implementation.</param>
        /// <returns>The observer object implemented using the given actions.</returns>
        public static IObserver<T> Create<T>(Action<T> onNext)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));

            return new AnonymousObserver<T>(onNext, ex => { }, () => { });
        }

        /// <summary>
        /// Creates an observer from the specified OnNext and OnError actions.
        /// </summary>
        /// <typeparam name="T">The type of the elements received by the observer.</typeparam>
        /// <param name="onNext">Observer's OnNext action implementation.</param>
        /// <param name="onError">Observer's OnError action implementation.</param>
        /// <returns>The observer object implemented using the given actions.</returns>
        public static IObserver<T> Create<T>(Action<T> onNext, Action<Exception> onError)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            return new AnonymousObserver<T>(onNext, onError, () => { });
        }

        /// <summary>
        /// Creates an observer from the specified OnNext and OnCompleted actions.
        /// </summary>
        /// <typeparam name="T">The type of the elements received by the observer.</typeparam>
        /// <param name="onNext">Observer's OnNext action implementation.</param>
        /// <param name="onCompleted">Observer's OnCompleted action implementation.</param>
        /// <returns>The observer object implemented using the given actions.</returns>
        public static IObserver<T> Create<T>(Action<T> onNext, Action onCompleted)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            return new AnonymousObserver<T>(onNext, ex => { }, onCompleted);
        }

        /// <summary>
        /// Creates an observer from the specified OnNext, OnError, and OnCompleted actions.
        /// </summary>
        /// <typeparam name="T">The type of the elements received by the observer.</typeparam>
        /// <param name="onNext">Observer's OnNext action implementation.</param>
        /// <param name="onError">Observer's OnError action implementation.</param>
        /// <param name="onCompleted">Observer's OnCompleted action implementation.</param>
        /// <returns>The observer object implemented using the given actions.</returns>
        public static IObserver<T> Create<T>(Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            return new AnonymousObserver<T>(onNext, onError, onCompleted);
        }

        private sealed class AnonymousObserver<T> : IObserver<T>
        {
            private readonly Action<T> _onNext;
            private readonly Action<Exception> _onError;
            private readonly Action _onCompleted;

            public AnonymousObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
            {
                _onNext = onNext;
                _onError = onError;
                _onCompleted = onCompleted;
            }

            public void OnNext(T value)
            {
                _onNext(value);
            }

            public void OnError(Exception error)
            {
                _onError(error);
            }

            public void OnCompleted()
            {
                _onCompleted();
            }
        }
    }
}
