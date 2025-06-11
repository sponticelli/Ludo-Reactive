using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Interface for managing dynamic computations with keys
    /// </summary>
    public interface IDynamicComputationManager
    {
        void ManageEffect<TKey>(TKey key, Action<ComputationBuilder> logic, params IObservable[] dependencies);
        void ManageComputed<TKey, T>(TKey key, Func<ComputationBuilder, T> computation, params IObservable[] dependencies);
        void ManageResource<TKey>(TKey key, SubscriptionHandle handle);
        void ManageDisposable<TKey, T>(TKey key, T disposable) where T : IDisposable;
        void Release<TKey>(TKey key);
    }
}