using System;
using System.Diagnostics;
using Ludo.Reactive.ErrorHandling;
using Ludo.Reactive.Logging;

namespace Ludo.Reactive
{
    /// <summary>
    /// Base class for all reactive computations with dependency tracking
    /// </summary>
    public abstract class ReactiveComputation : ResourceHierarchy
    {
        protected ReactiveScheduler _scheduler;
        protected DependencyTracker _dependencyTracker;
        protected bool _isPendingExecution;
        protected Exception _lastError;
        protected string _name;
        protected ErrorBoundary _errorBoundary;
        protected IReactiveLogger _logger;

        protected ReactiveComputation(string name, ReactiveScheduler scheduler, ErrorBoundary errorBoundary = null)
        {
            _name = name ?? GetType().Name;
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _dependencyTracker = new DependencyTracker(this);
            _errorBoundary = errorBoundary ?? ReactiveGlobals.GlobalErrorBoundary;
            _logger = ReactiveGlobals.Logger;
        }

        public ReactiveScheduler Scheduler => _scheduler;
        public string Name => _name;
        public Exception LastError => _lastError;
        public bool HasError => _lastError != null;

        internal void TrackDependency(IObservable dependency)
        {
            _dependencyTracker.AddDynamicDependency(dependency);
        }

        internal void ScheduleExecution()
        {
            if (!_isPendingExecution)
            {
                _isPendingExecution = true;
                _scheduler.Schedule(this);
            }
        }

        internal void ExecuteInternal()
        {
            if (!_isPendingExecution) return;

            _isPendingExecution = false;

            var stopwatch = Stopwatch.StartNew();
            var success = false;

            try
            {
                _errorBoundary.Execute(_name, () =>
                {
                    _dependencyTracker.BeginDynamicTracking();
                    ExecuteComputation();
                    _dependencyTracker.EndDynamicTracking();

                    // Update last known values after successful execution
                    _dependencyTracker.UpdateLastKnownValues();
                    success = true;
                });

                _lastError = null;
            }
            catch (Exception ex)
            {
                _lastError = ex;
                _logger?.LogException(ex, $"Unhandled exception in computation '{_name}'", new { ComputationName = _name });
            }
            finally
            {
                stopwatch.Stop();
                _logger?.LogComputationExecution(_name, stopwatch.Elapsed, success, _lastError);
            }
        }

        /// <summary>
        /// Checks if this computation has dirty dependencies
        /// </summary>
        internal bool HasDirtyDependencies()
        {
            return _dependencyTracker.HasDirtyDependencies();
        }

        protected abstract void ExecuteComputation();

        protected void AddStaticDependency(IObservable dependency)
        {
            _dependencyTracker.AddStaticDependency(dependency);
        }

        public override void Dispose()
        {
            _dependencyTracker.Dispose();
            base.Dispose();
        }
    }
}