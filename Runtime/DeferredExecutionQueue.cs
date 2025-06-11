using System;
using System.Collections.Generic;

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

        public bool IsEmpty => (_pendingActions?.Count ?? 0) == 0 && !_isExecuting;

        public void Initialize()
        {
            _pendingActions = new Queue<Action>();
            _holdCount = 0;
            _isExecuting = false;
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
            if (_pendingActions == null) Initialize();
            
            _pendingActions.Enqueue(action);
            if (_holdCount == 0 && !_isExecuting)
            {
                ExecutePendingActions();
            }
        }

        private void ExecutePendingActions()
        {
            if (_pendingActions == null) return;
            
            _isExecuting = true;
            try
            {
                while (_pendingActions.Count > 0)
                {
                    var action = _pendingActions.Dequeue();
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        // Log the exception but continue processing
                        Console.WriteLine($"Exception in deferred action: {ex}");
                    }
                }
            }
            finally
            {
                _isExecuting = false;
            }
        }
    }
}