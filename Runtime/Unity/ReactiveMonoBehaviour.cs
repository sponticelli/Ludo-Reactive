using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive.Unity
{
    /// <summary>
    /// Base MonoBehaviour with reactive capabilities
    /// </summary>
    public abstract class ReactiveMonoBehaviour : MonoBehaviour
    {
        private DynamicComputationManager _computationManager;
        private List<IDisposable> _disposables = new List<IDisposable>();

        protected DynamicComputationManager ComputationManager
        {
            get
            {
                if (_computationManager == null)
                {
                    _computationManager = new DynamicComputationManager(UnityReactiveScheduler.Instance);
                }

                return _computationManager;
            }
        }

        protected virtual void Awake()
        {
            InitializeReactive();
        }

        protected virtual void OnDestroy()
        {
            CleanupReactive();
        }

        protected virtual void InitializeReactive()
        {
        }

        protected ReactiveState<T> CreateState<T>(T initialValue = default(T))
        {
            var state = UnityReactiveFlow.CreateState(initialValue);
            _disposables.Add(state);
            return state;
        }

        protected ReactiveEffect CreateEffect(Action<ComputationBuilder> logic, params IObservable[] dependencies)
        {
            var effect = UnityReactiveFlow.CreateMainThreadEffect($"{gameObject.name}.Effect", logic, null, dependencies);
            _disposables.Add(effect);
            return effect;
        }

        protected ComputedValue<T> CreateComputed<T>(Func<ComputationBuilder, T> computation,
            params IObservable[] dependencies)
        {
            var computed =
                UnityReactiveFlow.CreateMainThreadComputed<T>($"{gameObject.name}.Computed", computation, default(T), null, dependencies);
            _disposables.Add(computed);
            return computed;
        }

        protected T ManageDisposable<T>(T disposable) where T : IDisposable
        {
            _disposables.Add(disposable);
            return disposable;
        }

        private void CleanupReactive()
        {
            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception disposing reactive resource: {ex}");
                }
            }

            _disposables.Clear();

            _computationManager?.Dispose();
            _computationManager = null;
        }
    }
}