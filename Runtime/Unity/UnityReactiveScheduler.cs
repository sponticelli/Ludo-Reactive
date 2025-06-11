using UnityEngine;
using System;
using System.Collections.Generic;

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

        static UnityReactiveScheduler()
        {
            // Ensure scheduler is updated every frame
            var updater = new GameObject("ReactiveFlow Updater");
            GameObject.DontDestroyOnLoad(updater);
            updater.AddComponent<UnityReactiveUpdater>();
        }

        public void ScheduleOnMainThread(Action action)
        {
            lock (_mainThreadActions)
            {
                _mainThreadActions.Enqueue(action);
            }
        }

        internal void ProcessMainThreadActions()
        {
            if (_isProcessingMainThread) return;

            _isProcessingMainThread = true;
            try
            {
                lock (_mainThreadActions)
                {
                    while (_mainThreadActions.Count > 0)
                    {
                        var action = _mainThreadActions.Dequeue();
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Exception in main thread action: {ex}");
                        }
                    }
                }
            }
            finally
            {
                _isProcessingMainThread = false;
            }
        }
    }
    
}