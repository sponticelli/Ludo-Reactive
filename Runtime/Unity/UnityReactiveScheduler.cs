using UnityEngine;
using System;
using System.Collections.Generic;
using Ludo.Reactive.Logging;

namespace Ludo.Reactive.Unity
{
    /// <summary>
    /// Unity-aware scheduler that ensures updates happen on the main thread
    /// </summary>
    public class UnityReactiveScheduler : ReactiveScheduler
    {
        private static UnityReactiveScheduler _instance;
        public static UnityReactiveScheduler Instance => _instance ??= new UnityReactiveScheduler();

        private Queue<Action> _mainThreadActions = new Queue<Action>();
        private bool _isProcessingMainThread = false;

        private UnityReactiveScheduler() : base(new UnityReactiveLogger())
        {
        }

        static UnityReactiveScheduler()
        {
            // Configure Unity-specific logging
            ReactiveGlobals.ConfigureLogger(new UnityReactiveLogger());

            // Ensure scheduler is updated every frame
            var updater = new GameObject("ReactiveFlow Updater");
            GameObject.DontDestroyOnLoad(updater);
            updater.AddComponent<UnityReactiveUpdater>();
        }

        public void ScheduleOnMainThread(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            lock (_mainThreadActions)
            {
                _mainThreadActions.Enqueue(action);
            }
        }

        internal void ProcessMainThreadActions()
        {
            if (_isProcessingMainThread) return;

            _isProcessingMainThread = true;
            var actionCount = 0;
            var errorCount = 0;

            try
            {
                lock (_mainThreadActions)
                {
                    actionCount = _mainThreadActions.Count;
                    while (_mainThreadActions.Count > 0)
                    {
                        var action = _mainThreadActions.Dequeue();
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            ReactiveGlobals.Logger?.LogException(ex, "Exception in Unity main thread action");
                        }
                    }
                }

                if (actionCount > 0)
                {
                    ReactiveGlobals.Logger?.LogDebug($"Processed {actionCount} main thread actions, {errorCount} errors");
                }
            }
            finally
            {
                _isProcessingMainThread = false;
            }
        }
    }
    
}