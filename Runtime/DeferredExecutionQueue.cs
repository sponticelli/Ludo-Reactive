using System;
using System.Collections.Generic;
using Ludo.Reactive.Logging;

namespace Ludo.Reactive
{
    /// <summary>
    /// Queue for deferred execution of actions
    /// </summary>
    public struct DeferredExecutionQueue
    {
        private int _holdCount;
        private Queue<Action> _pendingActions;
        private bool _isExecuting;
        private IReactiveLogger _logger;

        public bool IsEmpty => (_pendingActions?.Count ?? 0) == 0 && !_isExecuting;

        public void Initialize(IReactiveLogger logger = null)
        {
            _pendingActions = new Queue<Action>();
            _holdCount = 0;
            _isExecuting = false;
            _logger = logger ?? ReactiveGlobals.Logger;
        }

        public void Hold() => _holdCount++;

        public void Release()
        {
            if (_holdCount <= 0)
            {
                throw new InvalidOperationException("Mismatched Release() without Hold()");
            }
            if (--_holdCount == 0 && !_isExecuting)
            {
                ExecutePendingActions();
            }
        }

        public void Schedule(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (_pendingActions == null) Initialize();

            _pendingActions.Enqueue(action);
            _logger?.LogDebug($"Action scheduled. Queue size: {_pendingActions.Count}, Hold count: {_holdCount}");

            if (_holdCount == 0 && !_isExecuting)
            {
                ExecutePendingActions();
            }
        }

        private void ExecutePendingActions()
        {
            if (_pendingActions == null) return;

            var actionCount = _pendingActions.Count;
            _logger?.LogDebug($"Executing {actionCount} pending actions");

            _isExecuting = true;
            var successCount = 0;
            var errorCount = 0;

            try
            {
                while (_pendingActions.Count > 0)
                {
                    var action = _pendingActions.Dequeue();
                    try
                    {
                        action();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger?.LogException(ex, "Exception in deferred action", new { ActionIndex = successCount + errorCount });
                    }
                }
            }
            finally
            {
                _isExecuting = false;
                _logger?.LogDebug($"Deferred execution completed: {successCount} successful, {errorCount} errors");
            }
        }
    }
}