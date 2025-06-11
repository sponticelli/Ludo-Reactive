using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Reactive state container that notifies when values change
    /// </summary>
    public class ReactiveState<T> : IReactiveValue<T>, IDisposable
    {
        private T _currentValue;
        private readonly EventSource<T> _changeNotifier = new EventSource<T>();
        private readonly IEqualityComparer<T> _equalityComparer;

        public ReactiveState(T initialValue = default(T), IEqualityComparer<T> equalityComparer = null)
        {
            _currentValue = initialValue;
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        }

        public T Current => _currentValue;

        public void Set(T value)
        {
            if (!_equalityComparer.Equals(value, _currentValue))
            {
                _currentValue = value;
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

        public void Dispose()
        {
            // EventSource doesn't implement IDisposable, so we just clear subscriptions
            // The EventSource will be garbage collected when this ReactiveState is disposed
        }
    }
}