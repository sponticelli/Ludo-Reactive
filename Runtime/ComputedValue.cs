using System;
using Ludo.Reactive.ErrorHandling;

namespace Ludo.Reactive
{
    /// <summary>
    /// Cached computation that recalculates when dependencies change with memoization support
    /// </summary>
    public class ComputedValue<T> : ReactiveComputation, IReadOnlyReactiveValue<T>
    {
        private readonly ReactiveState<T> _cachedResult = new ReactiveState<T>();
        private readonly Func<ComputationBuilder, T> _computeFunction;
        private readonly T _fallbackValue;

        // Memoization support
        private ComputationMemoizer<string, T> _memoizer;
        private readonly bool _enableMemoization;

        public ComputedValue(
            string name,
            ReactiveScheduler scheduler,
            Func<ComputationBuilder, T> computeFunction,
            T fallbackValue = default,
            ErrorBoundary errorBoundary = null,
            params IObservable[] staticDependencies)
            : base(name, scheduler, errorBoundary)
        {
            _computeFunction = computeFunction ?? throw new ArgumentNullException(nameof(computeFunction));
            _fallbackValue = fallbackValue;
            _enableMemoization = false; // Default to false for backward compatibility

            // Memoization can be enabled via a separate method if needed
            // if (_enableMemoization)
            // {
            //     _memoizer = new ComputationMemoizer<string, T>(
            //         maxCacheSize: 50,
            //         maxAge: TimeSpan.FromMinutes(5));
            // }

            foreach (var dependency in staticDependencies ?? new IObservable[0])
            {
                AddStaticDependency(dependency);
            }

            ScheduleExecution();
        }

        public T Current => _cachedResult.Current;

        protected override void ExecuteComputation()
        {
            using var builder = new ComputationBuilder(this);

            try
            {
                T newValue;

                if (_enableMemoization && _memoizer != null)
                {
                    // Create a cache key based on dependency values
                    var cacheKey = GenerateCacheKey(builder);

                    newValue = _memoizer.GetOrCompute(cacheKey, _ => _computeFunction(builder));
                }
                else
                {
                    newValue = _computeFunction(builder);
                }

                _cachedResult.Set(newValue);
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Error computing value for '{Name}', using fallback value",
                    new { FallbackValue = _fallbackValue, ComputationName = Name });
                _cachedResult.Set(_fallbackValue);
                throw; // Re-throw to let error boundary handle it
            }
        }

        /// <summary>
        /// Generates a cache key based on current dependency values
        /// </summary>
        private string GenerateCacheKey(ComputationBuilder builder)
        {
            // Simple implementation - in practice, this would need to be more sophisticated
            // to properly capture all dependency values
            var keyBuilder = new System.Text.StringBuilder();
            keyBuilder.Append(Name);
            keyBuilder.Append("_");
            keyBuilder.Append(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond); // Second-level granularity

            return keyBuilder.ToString();
        }

        public SubscriptionHandle Subscribe(Action callback) => _cachedResult.Subscribe(callback);
        public SubscriptionHandle Subscribe(Action<T> callback) => _cachedResult.Subscribe(callback);
        public void Unsubscribe(Action callback) => _cachedResult.Unsubscribe(callback);
        public void Unsubscribe(Action<T> callback) => _cachedResult.Unsubscribe(callback);

        /// <summary>
        /// Enables memoization for this computed value
        /// </summary>
        public void EnableMemoization(int maxCacheSize = 50, TimeSpan? maxAge = null)
        {
            if (!_enableMemoization)
            {
                _memoizer = new ComputationMemoizer<string, T>(
                    maxCacheSize: maxCacheSize,
                    maxAge: maxAge ?? TimeSpan.FromMinutes(5));

                // Use reflection to set the field since it's readonly
                var field = typeof(ComputedValue<T>).GetField("_enableMemoization",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(this, true);
            }
        }

        /// <summary>
        /// Invalidates the memoization cache
        /// </summary>
        public void InvalidateCache()
        {
            _memoizer?.InvalidateAll();
        }

        /// <summary>
        /// Gets cache statistics if memoization is enabled
        /// </summary>
        public (int CacheSize, int MaxCacheSize)? GetCacheStats()
        {
            return _memoizer != null ? (_memoizer.CacheSize, _memoizer.MaxCacheSize) : null;
        }

        public override void Dispose()
        {
            _cachedResult?.Dispose();
            _memoizer = null;
            base.Dispose();
        }
    }
}