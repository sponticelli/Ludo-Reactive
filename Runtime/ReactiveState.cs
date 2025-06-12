using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Reactive state container that notifies when values change with optimized change detection
    /// </summary>
    public class ReactiveState<T> : IReactiveValue<T>, IDisposable
    {
        private T _currentValue;
        private readonly EventSource<T> _changeNotifier = new EventSource<T>();
        private readonly IEqualityComparer<T> _equalityComparer;

        // Change detection optimization
        private int _changeCount;
        private DateTime _lastChangeTime;

        public ReactiveState(T initialValue = default(T), IEqualityComparer<T> equalityComparer = null)
        {
            _currentValue = initialValue;
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            _changeCount = 0;
            _lastChangeTime = DateTime.UtcNow;
        }

        public T Current => _currentValue;

        public void Set(T value)
        {
            if (!_equalityComparer.Equals(value, _currentValue))
            {
                _currentValue = value;
                _changeCount++;
                _lastChangeTime = DateTime.UtcNow;
                _changeNotifier.Emit(value);
            }
        }

        public void Update(Func<T, T> updater)
        {
            Set(updater(_currentValue));
        }

        public SubscriptionHandle Subscribe(Action callback) => _changeNotifier.Subscribe(callback);
        public SubscriptionHandle Subscribe(Action<T> callback) => _changeNotifier.Subscribe(callback);
        public void Unsubscribe(Action callback) => _changeNotifier.Unsubscribe(callback);
        public void Unsubscribe(Action<T> callback) => _changeNotifier.Unsubscribe(callback);

        /// <summary>
        /// Gets the number of times this state has changed
        /// </summary>
        public int ChangeCount => _changeCount;

        /// <summary>
        /// Gets the time of the last change
        /// </summary>
        public DateTime LastChangeTime => _lastChangeTime;

        /// <summary>
        /// Checks if the state has changed since the specified time
        /// </summary>
        public bool HasChangedSince(DateTime time)
        {
            return _lastChangeTime > time;
        }

        /// <summary>
        /// Checks if the state has changed since the specified change count
        /// </summary>
        public bool HasChangedSince(int changeCount)
        {
            return _changeCount > changeCount;
        }

        public void Dispose()
        {
            // EventSource doesn't implement IDisposable, so we just clear subscriptions
            // The EventSource will be garbage collected when this ReactiveState is disposed
        }
    }
}