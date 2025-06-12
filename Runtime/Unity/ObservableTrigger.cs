using System;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// A MonoBehaviour component that provides observable sequences for Unity lifecycle events.
    /// This component is automatically added to GameObjects when using Unity lifecycle observables.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObservableTrigger : MonoBehaviour
    {
        private Subject<Unit> _onDestroySubject;
        private Subject<Unit> _onEnableSubject;
        private Subject<Unit> _onDisableSubject;
        private Subject<Unit> _updateSubject;
        private Subject<Unit> _fixedUpdateSubject;
        private Subject<Unit> _lateUpdateSubject;
        private Subject<Unit> _onApplicationPauseSubject;
        private Subject<Unit> _onApplicationFocusSubject;

        /// <summary>
        /// Gets an observable sequence that emits when the GameObject is destroyed.
        /// </summary>
        /// <returns>An observable sequence that emits when OnDestroy is called.</returns>
        public IObservable<Unit> OnDestroyAsObservable()
        {
            return _onDestroySubject ?? (_onDestroySubject = new Subject<Unit>());
        }

        /// <summary>
        /// Gets an observable sequence that emits when the GameObject is enabled.
        /// </summary>
        /// <returns>An observable sequence that emits when OnEnable is called.</returns>
        public IObservable<Unit> OnEnableAsObservable()
        {
            return _onEnableSubject ?? (_onEnableSubject = new Subject<Unit>());
        }

        /// <summary>
        /// Gets an observable sequence that emits when the GameObject is disabled.
        /// </summary>
        /// <returns>An observable sequence that emits when OnDisable is called.</returns>
        public IObservable<Unit> OnDisableAsObservable()
        {
            return _onDisableSubject ?? (_onDisableSubject = new Subject<Unit>());
        }

        /// <summary>
        /// Gets an observable sequence that emits every frame during Update.
        /// </summary>
        /// <returns>An observable sequence that emits during Update.</returns>
        public IObservable<Unit> UpdateAsObservable()
        {
            return _updateSubject ?? (_updateSubject = new Subject<Unit>());
        }

        /// <summary>
        /// Gets an observable sequence that emits every fixed frame during FixedUpdate.
        /// </summary>
        /// <returns>An observable sequence that emits during FixedUpdate.</returns>
        public IObservable<Unit> FixedUpdateAsObservable()
        {
            return _fixedUpdateSubject ?? (_fixedUpdateSubject = new Subject<Unit>());
        }

        /// <summary>
        /// Gets an observable sequence that emits every frame during LateUpdate.
        /// </summary>
        /// <returns>An observable sequence that emits during LateUpdate.</returns>
        public IObservable<Unit> LateUpdateAsObservable()
        {
            return _lateUpdateSubject ?? (_lateUpdateSubject = new Subject<Unit>());
        }

        /// <summary>
        /// Gets an observable sequence that emits when the application is paused or unpaused.
        /// </summary>
        /// <returns>An observable sequence that emits when OnApplicationPause is called.</returns>
        public IObservable<Unit> OnApplicationPauseAsObservable()
        {
            return _onApplicationPauseSubject ?? (_onApplicationPauseSubject = new Subject<Unit>());
        }

        /// <summary>
        /// Gets an observable sequence that emits when the application gains or loses focus.
        /// </summary>
        /// <returns>An observable sequence that emits when OnApplicationFocus is called.</returns>
        public IObservable<Unit> OnApplicationFocusAsObservable()
        {
            return _onApplicationFocusSubject ?? (_onApplicationFocusSubject = new Subject<Unit>());
        }

        private void OnDestroy()
        {
            try
            {
                _onDestroySubject?.OnNext(Unit.Default);
                _onDestroySubject?.OnCompleted();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Exception in OnDestroy observable: {ex}");
            }
            finally
            {
                DisposeAllSubjects();
            }
        }

        private void OnEnable()
        {
            try
            {
                _onEnableSubject?.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Exception in OnEnable observable: {ex}");
            }
        }

        private void OnDisable()
        {
            try
            {
                _onDisableSubject?.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Exception in OnDisable observable: {ex}");
            }
        }

        private void Update()
        {
            try
            {
                _updateSubject?.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Exception in Update observable: {ex}");
            }
        }

        private void FixedUpdate()
        {
            try
            {
                _fixedUpdateSubject?.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Exception in FixedUpdate observable: {ex}");
            }
        }

        private void LateUpdate()
        {
            try
            {
                _lateUpdateSubject?.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Exception in LateUpdate observable: {ex}");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            try
            {
                _onApplicationPauseSubject?.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Exception in OnApplicationPause observable: {ex}");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            try
            {
                _onApplicationFocusSubject?.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Exception in OnApplicationFocus observable: {ex}");
            }
        }

        private void DisposeAllSubjects()
        {
            try
            {
                _onDestroySubject?.Dispose();
                _onEnableSubject?.Dispose();
                _onDisableSubject?.Dispose();
                _updateSubject?.Dispose();
                _fixedUpdateSubject?.Dispose();
                _lateUpdateSubject?.Dispose();
                _onApplicationPauseSubject?.Dispose();
                _onApplicationFocusSubject?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Exception while disposing subjects: {ex}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // In editor, ensure we don't accumulate subjects during play mode changes
            if (!Application.isPlaying)
            {
                DisposeAllSubjects();
            }
        }
#endif
    }
}
