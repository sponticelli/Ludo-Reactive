using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents a read-only reactive property that can be observed for changes.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    public interface IReadOnlyReactiveProperty<out T> : IObservable<T>, IDisposable
    {
        /// <summary>
        /// Gets the current value of the property.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets a value indicating whether the property has observers.
        /// </summary>
        bool HasObservers { get; }

        /// <summary>
        /// Gets the number of observers subscribed to the property.
        /// </summary>
        int ObserverCount { get; }
    }

    /// <summary>
    /// Provides extension methods for IReadOnlyReactiveProperty.
    /// </summary>
    public static class ReadOnlyReactivePropertyExtensions
    {
        /// <summary>
        /// Converts a ReactiveProperty to a read-only reactive property.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="source">The source reactive property.</param>
        /// <returns>A read-only view of the reactive property.</returns>
        public static IReadOnlyReactiveProperty<T> AsReadOnly<T>(this ReactiveProperty<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new ReadOnlyReactivePropertyWrapper<T>(source);
        }

        /// <summary>
        /// Creates a read-only reactive property from an observable sequence with an initial value.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="source">The source observable sequence.</param>
        /// <param name="initialValue">The initial value of the property.</param>
        /// <returns>A read-only reactive property.</returns>
        public static IReadOnlyReactiveProperty<T> ToReadOnlyReactiveProperty<T>(
            this IObservable<T> source,
            T initialValue = default(T))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new ReadOnlyReactiveProperty<T>(source, initialValue);
        }
    }

    /// <summary>
    /// A wrapper that provides a read-only view of a ReactiveProperty.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    internal sealed class ReadOnlyReactivePropertyWrapper<T> : IReadOnlyReactiveProperty<T>
    {
        private readonly ReactiveProperty<T> _source;

        public ReadOnlyReactivePropertyWrapper(ReactiveProperty<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public T Value => _source.Value;
        public bool HasObservers => _source.HasObservers;
        public int ObserverCount => _source.ObserverCount;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _source.Subscribe(observer);
        }

        public void Dispose()
        {
            // Don't dispose the source, just this wrapper
        }
    }

    /// <summary>
    /// A read-only reactive property implementation that wraps an observable sequence.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    internal sealed class ReadOnlyReactiveProperty<T> : IReadOnlyReactiveProperty<T>
    {
        private readonly ReactiveProperty<T> _property;
        private readonly IDisposable _subscription;

        public ReadOnlyReactiveProperty(IObservable<T> source, T initialValue)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _property = new ReactiveProperty<T>(initialValue);
            _subscription = source.Subscribe(Observer.Create<T>(value => _property.Value = value));
        }

        public T Value => _property.Value;
        public bool HasObservers => _property.HasObservers;
        public int ObserverCount => _property.ObserverCount;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _property.Subscribe(observer);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _property?.Dispose();
        }
    }
}
