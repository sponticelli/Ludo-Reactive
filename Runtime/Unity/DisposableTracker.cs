using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// A MonoBehaviour component that automatically disposes of registered disposables when the GameObject is destroyed.
    /// This component is automatically added to GameObjects when using the AddTo extension method.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DisposableTracker : MonoBehaviour
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly object _lock = new object();
        private bool _disposed = false;

        /// <summary>
        /// Gets the number of disposables currently being tracked.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _disposed ? 0 : _disposables.Count;
                }
            }
        }

        /// <summary>
        /// Adds a disposable to be tracked and automatically disposed when this GameObject is destroyed.
        /// </summary>
        /// <param name="disposable">The disposable to track.</param>
        /// <exception cref="ArgumentNullException">Thrown when disposable is null.</exception>
        public void Add(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));

            lock (_lock)
            {
                if (_disposed)
                {
                    // If already disposed, dispose the new disposable immediately
                    disposable.Dispose();
                    return;
                }

                _disposables.Add(disposable);
            }
        }

        /// <summary>
        /// Removes a disposable from tracking without disposing it.
        /// </summary>
        /// <param name="disposable">The disposable to remove from tracking.</param>
        /// <returns>True if the disposable was found and removed; otherwise, false.</returns>
        public bool Remove(IDisposable disposable)
        {
            if (disposable == null)
                return false;

            lock (_lock)
            {
                if (_disposed)
                    return false;

                return _disposables.Remove(disposable);
            }
        }

        /// <summary>
        /// Manually disposes all tracked disposables and clears the tracking list.
        /// This is automatically called when the GameObject is destroyed.
        /// </summary>
        public void DisposeAll()
        {
            List<IDisposable> toDispose = null;

            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                toDispose = new List<IDisposable>(_disposables);
                _disposables.Clear();
            }

            // Dispose outside of lock to avoid potential deadlocks
            foreach (var disposable in toDispose)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Exception while disposing tracked disposable: {ex}");
                }
            }
        }

        /// <summary>
        /// Clears all tracked disposables without disposing them.
        /// Use this if you want to manually manage disposal.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposables.Clear();
                }
            }
        }

        private void OnDestroy()
        {
            DisposeAll();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // On some platforms, OnDestroy might not be called reliably
            // So we also dispose on application pause as a safety measure
            if (pauseStatus)
            {
                DisposeAll();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Additional safety measure for platforms where OnDestroy is unreliable
            if (!hasFocus)
            {
                DisposeAll();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // In editor, ensure we don't accumulate disposables during play mode changes
            if (!Application.isPlaying)
            {
                DisposeAll();
            }
        }
#endif
    }
}
