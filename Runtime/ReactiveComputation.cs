using System;

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

        protected ReactiveComputation(string name, ReactiveScheduler scheduler)
        {
            _name = name ?? GetType().Name;
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _dependencyTracker = new DependencyTracker(this);
        }

        public ReactiveScheduler Scheduler => _scheduler;
        public string Name => _name;

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
            
            try
            {
                _dependencyTracker.BeginDynamicTracking();
                ExecuteComputation();
                _dependencyTracker.EndDynamicTracking();
                _lastError = null;
            }
            catch (Exception ex)
            {
                _lastError = ex;
                Console.WriteLine($"Exception in computation '{_name}': {ex}");
            }
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