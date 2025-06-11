using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Cached computation that recalculates when dependencies change
    /// </summary>
    public class ComputedValue<T> : ReactiveComputation, IReadOnlyReactiveValue<T>
    {
        private readonly ReactiveState<T> _cachedResult = new ReactiveState<T>();
        private readonly Func<ComputationBuilder, T> _computeFunction;

        public ComputedValue(
            string name,
            ReactiveScheduler scheduler,
            Func<ComputationBuilder, T> computeFunction,
            params IObservable[] staticDependencies)
            : base(name, scheduler)
        {
            _computeFunction = computeFunction ?? throw new ArgumentNullException(nameof(computeFunction));
            
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
            var newValue = _computeFunction(builder);
            _cachedResult.Set(newValue);
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