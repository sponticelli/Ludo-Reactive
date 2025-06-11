using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Main entry point for the ReactiveFlow framework
    /// </summary>
    public static class ReactiveFlow
    {
        private static ReactiveScheduler _defaultScheduler = new ReactiveScheduler();

        public static ReactiveScheduler DefaultScheduler => _defaultScheduler;

        public static ReactiveState<T> CreateState<T>(T initialValue = default(T))
        {
            return new ReactiveState<T>(initialValue);
        }

        public static ReactiveEffect CreateEffect(
            string name,
            Action<ComputationBuilder> logic,
            params IObservable[] dependencies)
        {
            return new ReactiveEffect(name, _defaultScheduler, logic, dependencies);
        }

        public static ComputedValue<T> CreateComputed<T>(
            string name,
            Func<ComputationBuilder, T> computation,
            params IObservable[] dependencies)
        {
            return new ComputedValue<T>(name, _defaultScheduler, computation, dependencies);
        }

        public static ConditionalComputation CreateConditional(
            string name,
            IReadOnlyReactiveValue<bool> condition,
            Action<ComputationBuilder> logic,
            params IObservable[] dependencies)
        {
            return new ConditionalComputation(name, _defaultScheduler, condition, logic, dependencies);
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