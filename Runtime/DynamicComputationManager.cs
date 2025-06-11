using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Manages computations with dynamic keys
    /// </summary>
    public class DynamicComputationManager : ResourceHierarchy, IDynamicComputationManager
    {
        private readonly Dictionary<object, SubscriptionHandle> _managedHandles = new Dictionary<object, SubscriptionHandle>();
        private readonly Dictionary<object, IDisposable> _managedDisposables = new Dictionary<object, IDisposable>();
        private readonly ReactiveScheduler _scheduler;

        public DynamicComputationManager(ReactiveScheduler scheduler)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public void ManageEffect<TKey>(TKey key, Action<ComputationBuilder> logic, params IObservable[] dependencies)
        {
            Release(key);
            var effect = new ReactiveEffect($"DynamicEffect-{key}", _scheduler, logic, dependencies);
            ManageDisposable(key, effect);
        }

        public void ManageComputed<TKey, T>(TKey key, Func<ComputationBuilder, T> computation, params IObservable[] dependencies)
        {
            Release(key);
            var computed = new ComputedValue<T>($"DynamicComputed-{key}", _scheduler, computation, dependencies);
            ManageDisposable(key, computed);
        }

        public void ManageResource<TKey>(TKey key, SubscriptionHandle handle)
        {
            Release(key);
            _managedHandles[key] = handle;
        }

        public void ManageDisposable<TKey, T>(TKey key, T disposable) where T : IDisposable
        {
            Release(key);
            _managedDisposables[key] = disposable;
        }

        public void Release<TKey>(TKey key)
        {
            if (_managedHandles.TryGetValue(key, out var handle))
            {
                handle?.Dispose();
                _managedHandles.Remove(key);
            }
            
            if (_managedDisposables.TryGetValue(key, out var disposable))
            {
                disposable?.Dispose();
                _managedDisposables.Remove(key);
            }
        }

        public override void Dispose()
        {
            foreach (var handle in _managedHandles.Values)
            {
                handle?.Dispose();
            }
            
            foreach (var disposable in _managedDisposables.Values)
            {
                disposable?.Dispose();
            }
            
            _managedHandles.Clear();
            _managedDisposables.Clear();
            
            base.Dispose();
        }
    }
}