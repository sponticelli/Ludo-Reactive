using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Provides API for building reactive computations with automatic dependency tracking
    /// </summary>
    public class ComputationBuilder : IDisposable
    {
        private ReactiveComputation _owner;
        private bool _isSealed;

        internal ComputationBuilder(ReactiveComputation owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public ReactiveScheduler Scheduler => _owner.Scheduler;

        // Automatic dependency tracking
        public T Track<T>(IReadOnlyReactiveValue<T> reactiveValue)
        {
            EnsureNotSealed();
            _owner.TrackDependency(reactiveValue);
            return reactiveValue.Current;
        }

        // Resource management
        public SubscriptionHandle Use(SubscriptionHandle handle)
        {
            EnsureNotSealed();
            return _owner.ManageResource(handle);
        }

        public T Use<T>(T disposable) where T : IDisposable
        {
            EnsureNotSealed();
            return _owner.ManageResource(disposable);
        }

        // Nested computations
        public IReadOnlyReactiveValue<T> CreateComputed<T>(
            Func<ComputationBuilder, T> computation,
            params IObservable[] dependencies)
        {
            EnsureNotSealed();
            return Use(new ComputedValue<T>($"{_owner.Name}.Computed", _owner.Scheduler, computation, default(T), null, dependencies));
        }

        public void CreateEffect(
            Action<ComputationBuilder> logic,
            params IObservable[] dependencies)
        {
            EnsureNotSealed();
            Use(new ReactiveEffect($"{_owner.Name}.Effect", _owner.Scheduler, logic, null, dependencies));
        }

        // Context management
        public T CreateContext<T>(T value)
        {
            EnsureNotSealed();
            _owner.SetContext<T>(value);
            return value;
        }

        public T GetContext<T>() => _owner.GetContext<T>();

        // Cleanup registration
        public void OnCleanup(Action cleanupAction)
        {
            EnsureNotSealed();
            _owner.RegisterCleanup(cleanupAction);
        }

        private void EnsureNotSealed()
        {
            if (_isSealed)
            {
                throw new InvalidOperationException("ComputationBuilder has been sealed and cannot be used");
            }
        }

        public void Dispose()
        {
            _isSealed = true;
        }
    }
}