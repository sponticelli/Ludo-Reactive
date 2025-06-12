using System;
using System.Collections.Generic;
using System.Linq;
using Ludo.Reactive.ErrorHandling;
using Ludo.Reactive.Logging;

namespace Ludo.Reactive
{
    /// <summary>
    /// Orchestrates computation execution with dependency-aware scheduling and batch optimization
    /// </summary>
    public class ReactiveScheduler
    {
        private SchedulingMode _mode = SchedulingMode.Immediate;
        private HashSet<ReactiveComputation> _scheduledComputations = new HashSet<ReactiveComputation>();
        private DeferredExecutionQueue _deferredQueue;
        private int _maxIterations = 1000;
        private readonly IReactiveLogger _logger;
        private readonly ErrorBoundary _schedulerErrorBoundary;

        // Batch processing optimization
        private Dictionary<ReactiveComputation, int> _computationDepths = new Dictionary<ReactiveComputation, int>();
        private bool _depthCacheInvalid = true;

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
            _depthCacheInvalid = true; // Invalidate depth cache when new computation is scheduled
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

                    // Sort by dependency depth for optimal execution order
                    currentBatch = OptimizeBatchExecutionOrder(currentBatch);

                    var successCount = 0;
                    var errorCount = 0;

                    foreach (var computation in currentBatch)
                    {
                        try
                        {
                            // Skip execution if no dependencies have changed (dirty checking optimization)
                            if (ShouldSkipExecution(computation))
                            {
                                _logger?.LogSchedulerEvent("SkipExecution", $"Skipping computation '{computation.Name}' - no dirty dependencies");
                                successCount++;
                                continue;
                            }

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

        /// <summary>
        /// Optimizes batch execution order based on dependency depth
        /// </summary>
        private List<ReactiveComputation> OptimizeBatchExecutionOrder(List<ReactiveComputation> batch)
        {
            if (batch.Count <= 1) return batch;

            UpdateDepthCache(batch);

            // Sort by depth (shallow dependencies first)
            batch.Sort((a, b) =>
            {
                var depthA = _computationDepths.GetValueOrDefault(a, 0);
                var depthB = _computationDepths.GetValueOrDefault(b, 0);
                return depthA.CompareTo(depthB);
            });

            return batch;
        }

        /// <summary>
        /// Updates the dependency depth cache for computations
        /// </summary>
        private void UpdateDepthCache(List<ReactiveComputation> computations)
        {
            if (!_depthCacheInvalid) return;

            _computationDepths.Clear();

            foreach (var computation in computations)
            {
                _computationDepths[computation] = CalculateDependencyDepth(computation);
            }

            _depthCacheInvalid = false;
        }

        /// <summary>
        /// Calculates the dependency depth of a computation
        /// </summary>
        private int CalculateDependencyDepth(ReactiveComputation computation)
        {
            // Simple heuristic: use the computation's hierarchy level
            // In a more sophisticated implementation, this would traverse the actual dependency graph
            var depth = 0;
            var current = computation;

            // Count parent hierarchy levels
            while (current?.Parent != null)
            {
                depth++;
                current = current.Parent as ReactiveComputation;
                if (depth > 10) break; // Prevent infinite loops
            }

            return depth;
        }

        /// <summary>
        /// Determines if a computation should skip execution based on dirty checking
        /// </summary>
        private bool ShouldSkipExecution(ReactiveComputation computation)
        {
            // Check if the computation has dirty dependencies
            return !computation.HasDirtyDependencies();
        }

        public void Dispose()
        {
            _schedulerErrorBoundary?.Dispose();
            _scheduledComputations.Clear();
            _computationDepths.Clear();
        }
    }
}