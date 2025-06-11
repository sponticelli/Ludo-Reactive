using System;
using Ludo.Reactive.ErrorHandling;

namespace Ludo.Reactive.Unity
{
    /// <summary>
    /// Unity-specific reactive utilities
    /// </summary>
    public static class UnityReactiveFlow
    {
        public static UnityReactiveScheduler Scheduler => UnityReactiveScheduler.Instance;

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
            return new ReactiveEffect(name, Scheduler, logic, errorBoundary, dependencies);
        }

        public static ComputedValue<T> CreateComputed<T>(
            string name,
            Func<ComputationBuilder, T> computation,
            T fallbackValue = default,
            ErrorBoundary errorBoundary = null,
            params IObservable[] dependencies)
        {
            return new ComputedValue<T>(name, Scheduler, computation, fallbackValue, errorBoundary, dependencies);
        }

        /// <summary>
        /// Creates an effect that runs on Unity's main thread
        /// </summary>
        public static ReactiveEffect CreateMainThreadEffect(
            string name,
            Action<ComputationBuilder> logic,
            ErrorBoundary errorBoundary = null,
            params IObservable[] dependencies)
        {
            return new ReactiveEffect(name, Scheduler,
                builder => { Scheduler.ScheduleOnMainThread(() => logic(builder)); }, errorBoundary, dependencies);
        }

        /// <summary>
        /// Creates a computed value that ensures computation runs on main thread
        /// </summary>
        public static ComputedValue<T> CreateMainThreadComputed<T>(
            string name,
            Func<ComputationBuilder, T> computation,
            T fallbackValue = default,
            ErrorBoundary errorBoundary = null,
            params IObservable[] dependencies)
        {
            var result = CreateState<T>();

            CreateMainThreadEffect($"{name}.Effect", builder =>
            {
                var value = computation(builder);
                result.Set(value);
            }, errorBoundary, dependencies);

            return new ComputedValue<T>(name, Scheduler, builder => builder.Track(result), fallbackValue, errorBoundary, result);
        }
    }
}