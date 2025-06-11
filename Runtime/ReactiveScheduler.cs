using System;
using System.Collections.Generic;
using System.Linq;
using Ludo.Reactive.ErrorHandling;
using Ludo.Reactive.Logging;

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
        private readonly IReactiveLogger _logger;
        private readonly ErrorBoundary _schedulerErrorBoundary;

        public DeferredExecutionQueue DeferredQueue => _deferredQueue;

        public ReactiveScheduler(IReactiveLogger logger = null)
        {
            _deferredQueue.Initialize();
            _logger = logger ?? ReactiveGlobals.Logger;
            _schedulerErrorBoundary = new ErrorBoundary("Scheduler", _logger);
        }

        public void Schedule(ReactiveComputation computation)
        {
            if (computation == null) throw new ArgumentNullException(nameof(computation));

            _scheduledComputations.Add(computation);
            _logger?.LogSchedulerEvent("Schedule", $"Computation '{computation.Name}' scheduled for execution");

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
            _schedulerErrorBoundary.Execute("ExecuteScheduledComputations", () =>
            {
                var iterations = 0;

                while (_scheduledComputations.Count > 0)
                {
                    if (++iterations > _maxIterations)
                    {
                        var errorMsg = $"Maximum iteration limit ({_maxIterations}) exceeded - possible infinite loop detected";
                        _logger?.LogError(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }

                    var currentBatch = _scheduledComputations.ToList();
                    _scheduledComputations.Clear();

                    _logger?.LogSchedulerEvent("ExecuteBatch", $"Executing batch of {currentBatch.Count} computations (iteration {iterations})");

                    // Sort by hierarchy (deepest first for proper cleanup order)
                    currentBatch.Sort();

                    var successCount = 0;
                    var errorCount = 0;

                    foreach (var computation in currentBatch)
                    {
                        try
                        {
                            computation.ExecuteInternal();
                            if (!computation.HasError)
                                successCount++;
                            else
                                errorCount++;
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            _logger?.LogException(ex, $"Critical error executing computation '{computation.Name}'",
                                new { ComputationName = computation.Name, Iteration = iterations });
                        }
                    }

                    _logger?.LogSchedulerEvent("BatchComplete", $"Batch completed: {successCount} successful, {errorCount} errors");
                }
            });
        }

        /// <summary>
        /// Sets the maximum number of iterations before detecting infinite loops
        /// </summary>
        public void SetMaxIterations(int maxIterations)
        {
            if (maxIterations <= 0) throw new ArgumentOutOfRangeException(nameof(maxIterations));
            _maxIterations = maxIterations;
            _logger?.LogInfo($"Max iterations set to {maxIterations}");
        }

        /// <summary>
        /// Gets the current scheduling mode
        /// </summary>
        public SchedulingMode Mode => _mode;

        /// <summary>
        /// Sets the scheduling mode
        /// </summary>
        public void SetMode(SchedulingMode mode)
        {
            _mode = mode;
            _logger?.LogInfo($"Scheduling mode set to {mode}");
        }

        /// <summary>
        /// Gets the number of currently scheduled computations
        /// </summary>
        public int ScheduledCount => _scheduledComputations.Count;

        public void Dispose()
        {
            _schedulerErrorBoundary?.Dispose();
            _scheduledComputations.Clear();
        }
    }
}