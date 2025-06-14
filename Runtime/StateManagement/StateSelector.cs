using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive.StateManagement
{
    /// <summary>
    /// Represents a function that selects a portion of the state.
    /// Selectors are used to efficiently extract and transform state data.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    /// <typeparam name="TResult">The type of the selected result.</typeparam>
    public interface IStateSelector<in TState, out TResult>
    {
        /// <summary>
        /// Gets the name of this selector for debugging purposes.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Selects a portion of the state.
        /// </summary>
        /// <param name="state">The state to select from.</param>
        /// <returns>The selected result.</returns>
        TResult Select(TState state);
    }

    /// <summary>
    /// A memoized state selector that caches results to improve performance.
    /// Only recalculates when the input state changes.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    /// <typeparam name="TResult">The type of the selected result.</typeparam>
    public class MemoizedSelector<TState, TResult> : IStateSelector<TState, TResult>, IDisposable
    {
        private readonly Func<TState, TResult> _selector;
        private readonly IEqualityComparer<TState> _stateComparer;
        private readonly IEqualityComparer<TResult> _resultComparer;
        private readonly string _name;

        private TState _lastState;
        private TResult _lastResult;
        private bool _hasResult;
        private bool _disposed;

        /// <inheritdoc />
        public string Name => _name;

        /// <summary>
        /// Gets the number of times this selector has been called.
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        /// Gets the number of times this selector returned a cached result.
        /// </summary>
        public int CacheHitCount { get; private set; }

        /// <summary>
        /// Gets the cache hit ratio as a percentage.
        /// </summary>
        public float CacheHitRatio => CallCount > 0 ? (float)CacheHitCount / CallCount : 0f;

        /// <summary>
        /// Initializes a new instance of the MemoizedSelector class.
        /// </summary>
        /// <param name="selector">The selector function.</param>
        /// <param name="name">The name of this selector.</param>
        /// <param name="stateComparer">The comparer for state equality.</param>
        /// <param name="resultComparer">The comparer for result equality.</param>
        public MemoizedSelector(
            Func<TState, TResult> selector,
            string name = null,
            IEqualityComparer<TState> stateComparer = null,
            IEqualityComparer<TResult> resultComparer = null)
        {
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _name = name ?? $"Selector_{GetHashCode():X8}";
            _stateComparer = stateComparer ?? EqualityComparer<TState>.Default;
            _resultComparer = resultComparer ?? EqualityComparer<TResult>.Default;
        }

        /// <inheritdoc />
        public TResult Select(TState state)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MemoizedSelector<TState, TResult>));

            CallCount++;

            // Check if we can return cached result
            if (_hasResult && _stateComparer.Equals(_lastState, state))
            {
                CacheHitCount++;
                return _lastResult;
            }

            // Calculate new result
            try
            {
                var result = _selector(state);
                
                // Update cache
                _lastState = state;
                _lastResult = result;
                _hasResult = true;

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Selector '{Name}' threw exception: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Clears the cached result, forcing recalculation on next call.
        /// </summary>
        public void ClearCache()
        {
            _hasResult = false;
            _lastState = default;
            _lastResult = default;
        }

        /// <summary>
        /// Resets the performance counters.
        /// </summary>
        public void ResetCounters()
        {
            CallCount = 0;
            CacheHitCount = 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ClearCache();
        }

        /// <summary>
        /// Returns a string representation of this selector.
        /// </summary>
        /// <returns>A string representation of this selector.</returns>
        public override string ToString()
        {
            return $"{Name} (Calls: {CallCount}, Cache Hit Ratio: {CacheHitRatio:P1})";
        }
    }

    /// <summary>
    /// A reactive selector that emits changes when the selected result changes.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    /// <typeparam name="TResult">The type of the selected result.</typeparam>
    public class ReactiveSelector<TState, TResult> : IObservable<TResult>, IDisposable
    {
        private readonly IObservable<TState> _stateObservable;
        private readonly MemoizedSelector<TState, TResult> _selector;
        private readonly ReplaySubject<TResult> _subject;
        private readonly IEqualityComparer<TResult> _resultComparer;
        private IDisposable _subscription;
        private bool _disposed;

        /// <summary>
        /// Gets the name of this selector.
        /// </summary>
        public string Name => _selector.Name;

        /// <summary>
        /// Gets the performance metrics for this selector.
        /// </summary>
        public MemoizedSelector<TState, TResult> Selector => _selector;

        /// <summary>
        /// Initializes a new instance of the ReactiveSelector class.
        /// </summary>
        /// <param name="stateObservable">The observable state stream.</param>
        /// <param name="selector">The selector function.</param>
        /// <param name="name">The name of this selector.</param>
        /// <param name="resultComparer">The comparer for result equality.</param>
        public ReactiveSelector(
            IObservable<TState> stateObservable,
            Func<TState, TResult> selector,
            string name = null,
            IEqualityComparer<TResult> resultComparer = null)
        {
            _stateObservable = stateObservable ?? throw new ArgumentNullException(nameof(stateObservable));
            _selector = new MemoizedSelector<TState, TResult>(selector, name);
            _subject = new ReplaySubject<TResult>(bufferSize: 1);
            _resultComparer = resultComparer ?? EqualityComparer<TResult>.Default;

            Initialize();
        }

        private void Initialize()
        {
            TResult lastResult = default;
            bool hasLastResult = false;

            _subscription = _stateObservable.Subscribe(
                state =>
                {
                    try
                    {
                        var result = _selector.Select(state);

                        // Only emit if result changed
                        if (!hasLastResult || !_resultComparer.Equals(lastResult, result))
                        {
                            lastResult = result;
                            hasLastResult = true;
                            _subject.OnNext(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _subject.OnError(ex);
                    }
                },
                error => _subject.OnError(error),
                () => _subject.OnCompleted()
            );
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ReactiveSelector<TState, TResult>));

            return _subject.Subscribe(observer);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _subscription?.Dispose();
            _selector?.Dispose();
            _subject?.Dispose();
        }
    }

    /// <summary>
    /// Provides extension methods for creating and working with state selectors.
    /// </summary>
    public static class StateSelectorExtensions
    {
        /// <summary>
        /// Creates a memoized selector from a function.
        /// </summary>
        /// <typeparam name="TState">The type of the application state.</typeparam>
        /// <typeparam name="TResult">The type of the selected result.</typeparam>
        /// <param name="selector">The selector function.</param>
        /// <param name="name">The name of the selector.</param>
        /// <returns>A memoized selector.</returns>
        public static MemoizedSelector<TState, TResult> Memoize<TState, TResult>(
            this Func<TState, TResult> selector,
            string name = null)
        {
            return new MemoizedSelector<TState, TResult>(selector, name);
        }

        /// <summary>
        /// Creates a reactive selector that emits when the selected result changes.
        /// </summary>
        /// <typeparam name="TState">The type of the application state.</typeparam>
        /// <typeparam name="TResult">The type of the selected result.</typeparam>
        /// <param name="stateObservable">The observable state stream.</param>
        /// <param name="selector">The selector function.</param>
        /// <param name="name">The name of the selector.</param>
        /// <returns>A reactive selector.</returns>
        public static ReactiveSelector<TState, TResult> Select<TState, TResult>(
            this IObservable<TState> stateObservable,
            Func<TState, TResult> selector,
            string name = null)
        {
            return new ReactiveSelector<TState, TResult>(stateObservable, selector, name);
        }
    }
}
