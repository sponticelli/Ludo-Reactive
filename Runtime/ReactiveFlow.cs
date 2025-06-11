using System;
using Ludo.Reactive.ErrorHandling;

namespace Ludo.Reactive
{
    /// <summary>
    /// Main entry point for the ReactiveFlow framework
    /// </summary>
    public static class ReactiveFlow
    {
        private static ReactiveScheduler _defaultScheduler = new ReactiveScheduler();

        public static ReactiveScheduler DefaultScheduler => _defaultScheduler;

        /// <summary>
        /// Sets the default scheduler for the framework
        /// </summary>
        public static void SetDefaultScheduler(ReactiveScheduler scheduler)
        {
            _defaultScheduler?.Dispose();
            _defaultScheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public static ReactiveState<T> CreateState<T>(T initialValue = default(T))
        {
            return new ReactiveState<T>(initialValue);
        }

        public static ReactiveEffect CreateEffect(
            string name,
            Action<ComputationBuilder> logic,
            ErrorBoundary errorBoundary = null,
            params IObservable[] dependencies)
        {
            return new ReactiveEffect(name, _defaultScheduler, logic, errorBoundary, dependencies);
        }

        public static ComputedValue<T> CreateComputed<T>(
            string name,
            Func<ComputationBuilder, T> computation,
            T fallbackValue = default,
            ErrorBoundary errorBoundary = null,
            params IObservable[] dependencies)
        {
            return new ComputedValue<T>(name, _defaultScheduler, computation, fallbackValue, errorBoundary, dependencies);
        }

        public static ConditionalComputation CreateConditional(
            string name,
            IReadOnlyReactiveValue<bool> condition,
            Action<ComputationBuilder> logic,
            ErrorBoundary errorBoundary = null,
            params IObservable[] dependencies)
        {
            return new ConditionalComputation(name, _defaultScheduler, condition, logic, errorBoundary, dependencies);
        }

        public static DynamicComputationManager CreateDynamicManager()
        {
            return new DynamicComputationManager(_defaultScheduler);
        }

        public static void ExecuteBatch(Action batchedOperations)
        {
            _defaultScheduler.ExecuteBatch(batchedOperations);
        }
    }
}