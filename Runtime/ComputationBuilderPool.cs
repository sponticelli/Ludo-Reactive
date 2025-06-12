using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Pool manager for ComputationBuilder instances to reduce GC pressure
    /// </summary>
    internal static class ComputationBuilderPool
    {
        private static ObjectPool<PooledComputationBuilder> _pool = ObjectPool<PooledComputationBuilder>.Create(
            createAction: () => new PooledComputationBuilder(),
            resetAction: builder => builder.Reset(),
            maxSize: 50
        );

        public static PooledComputationBuilder Rent(ReactiveComputation owner)
        {
            var builder = _pool.Rent();
            builder.Initialize(owner);
            return builder;
        }

        public static void Return(PooledComputationBuilder builder)
        {
            if (builder != null)
            {
                _pool.Return(builder);
            }
        }
    }

    /// <summary>
    /// Simple pooled wrapper for ComputationBuilder functionality
    /// Note: This is a simplified implementation. For production use,
    /// consider making ComputationBuilder fields protected or adding proper pooling support.
    /// </summary>
    internal class PooledComputationBuilder : IDisposable
    {
        private ComputationBuilder _builder;
        private bool _isRented;

        public PooledComputationBuilder()
        {
            _isRented = false;
        }

        internal void Initialize(ReactiveComputation owner)
        {
            if (_isRented)
            {
                throw new InvalidOperationException("PooledComputationBuilder is already in use");
            }

            _builder = new ComputationBuilder(owner);
            _isRented = true;
        }

        internal void Reset()
        {
            if (_isRented)
            {
                _builder?.Dispose();
                _builder = null;
                _isRented = false;
            }
        }

        // Delegate all ComputationBuilder methods
        public ReactiveScheduler Scheduler => _builder?.Scheduler;

        public T Track<T>(IReadOnlyReactiveValue<T> reactiveValue)
        {
            if (!_isRented || _builder == null)
                throw new InvalidOperationException("PooledComputationBuilder is not properly initialized");
            return _builder.Track(reactiveValue);
        }

        public SubscriptionHandle Use(SubscriptionHandle handle)
        {
            if (!_isRented || _builder == null)
                throw new InvalidOperationException("PooledComputationBuilder is not properly initialized");
            return _builder.Use(handle);
        }

        public T Use<T>(T disposable) where T : IDisposable
        {
            if (!_isRented || _builder == null)
                throw new InvalidOperationException("PooledComputationBuilder is not properly initialized");
            return _builder.Use(disposable);
        }

        public IReadOnlyReactiveValue<T> CreateComputed<T>(
            Func<ComputationBuilder, T> computation,
            params IObservable[] dependencies)
        {
            if (!_isRented || _builder == null)
                throw new InvalidOperationException("PooledComputationBuilder is not properly initialized");
            return _builder.CreateComputed(computation, dependencies);
        }

        public void CreateEffect(
            Action<ComputationBuilder> logic,
            params IObservable[] dependencies)
        {
            if (!_isRented || _builder == null)
                throw new InvalidOperationException("PooledComputationBuilder is not properly initialized");
            _builder.CreateEffect(logic, dependencies);
        }

        public T CreateContext<T>(T value)
        {
            if (!_isRented || _builder == null)
                throw new InvalidOperationException("PooledComputationBuilder is not properly initialized");
            return _builder.CreateContext(value);
        }

        public T GetContext<T>()
        {
            if (!_isRented || _builder == null)
                throw new InvalidOperationException("PooledComputationBuilder is not properly initialized");
            return _builder.GetContext<T>();
        }

        public void OnCleanup(Action cleanupAction)
        {
            if (!_isRented || _builder == null)
                throw new InvalidOperationException("PooledComputationBuilder is not properly initialized");
            _builder.OnCleanup(cleanupAction);
        }

        public void Dispose()
        {
            if (_isRented)
            {
                // Return to pool instead of disposing
                ComputationBuilderPool.Return(this);
            }
        }
    }
}
