using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludo.Reactive
{
    /// <summary>
    /// Orchestrates computation execution with dependency-aware scheduling
    /// </summary>
    public class ReactiveScheduler
    {
        private SchedulingMode _mode = SchedulingMode.Immediate;
        private HashSet<ReactiveComputation> _scheduledComputations = new HashSet<ReactiveComputation>();
        private DeferredExecutionQueue _deferredQueue;
        private int _maxIterations = 1000;

        public DeferredExecutionQueue DeferredQueue => _deferredQueue;

        public ReactiveScheduler()
        {
            _deferredQueue.Initialize();
        }

        public void Schedule(ReactiveComputation computation)
        {
            _scheduledComputations.Add(computation);
            if (_mode == SchedulingMode.Immediate)
            {
                _deferredQueue.Schedule(ExecuteScheduledComputations);
            }
        }

        public void ExecuteBatch(Action batchedOperations)
        {
            _deferredQueue.Hold();
            try
            {
                batchedOperations();
            }
            finally
            {
                _deferredQueue.Release();
            }
        }

        private void ExecuteScheduledComputations()
        {
            var iterations = 0;
            
            while (_scheduledComputations.Count > 0)
            {
                if (++iterations > _maxIterations)
                {
                    throw new InvalidOperationException("Maximum iteration limit exceeded - possible infinite loop detected");
                }
                
                var currentBatch = _scheduledComputations.ToList();
                _scheduledComputations.Clear();
                
                // Sort by hierarchy (deepest first for proper cleanup order)
                currentBatch.Sort();
                
                foreach (var computation in currentBatch)
                {
                    try
                    {
                        computation.ExecuteInternal();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception executing computation: {ex}");
                    }
                }
            }
        }
    }
}