using System;
using Ludo.Reactive.ErrorHandling;

namespace Ludo.Reactive
{
    /// <summary>
    /// Cached computation that recalculates when dependencies change
    /// </summary>
    public class ComputedValue<T> : ReactiveComputation, IReadOnlyReactiveValue<T>
    {
        private readonly ReactiveState<T> _cachedResult = new ReactiveState<T>();
        private readonly Func<ComputationBuilder, T> _computeFunction;
        private readonly T _fallbackValue;

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
                var newValue = _computeFunction(builder);
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

        public SubscriptionHandle Subscribe(Action callback) => _cachedResult.Subscribe(callback);
        public SubscriptionHandle Subscribe(Action<T> callback) => _cachedResult.Subscribe(callback);
        public void Unsubscribe(Action callback) => _cachedResult.Unsubscribe(callback);
        public void Unsubscribe(Action<T> callback) => _cachedResult.Unsubscribe(callback);

        public override void Dispose()
        {
            _cachedResult?.Dispose();
            base.Dispose();
        }
    }
}