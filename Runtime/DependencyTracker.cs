using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Tracks static and dynamic dependencies for computations
    /// </summary>
    public struct DependencyTracker : IDisposable
    {
        private ReactiveComputation _owner;
        private HashSet<IObservable> _trackedDependencies;
        private List<(IObservable observable, SubscriptionHandle handle)> _staticSubscriptions;
        private List<(IObservable observable, SubscriptionHandle handle)> _dynamicSubscriptions;
        private int _currentDynamicIndex;

        public DependencyTracker(ReactiveComputation owner)
        {
            _owner = owner;
            _trackedDependencies = new HashSet<IObservable>();
            _staticSubscriptions = new List<(IObservable, SubscriptionHandle)>();
            _dynamicSubscriptions = new List<(IObservable, SubscriptionHandle)>();
            _currentDynamicIndex = 0;
        }

        public void AddStaticDependency(IObservable observable)
        {
            if (ShouldTrack(observable))
            {
                var handle = observable.Subscribe(CreateScheduleCallback());
                _staticSubscriptions.Add((observable, handle));
                _trackedDependencies.Add(observable);
            }
        }

        public void BeginDynamicTracking()
        {
            _currentDynamicIndex = 0;
            _trackedDependencies.Clear();
            
            // Add static dependencies to tracked set
            foreach (var (observable, _) in _staticSubscriptions)
            {
                _trackedDependencies.Add(observable);
            }
        }

        public void AddDynamicDependency(IObservable observable)
        {
            if (!ShouldTrack(observable)) return;
            
            if (_currentDynamicIndex >= _dynamicSubscriptions.Count)
            {
                // Add new subscription
                var handle = observable.Subscribe(CreateScheduleCallback());
                _dynamicSubscriptions.Add((observable, handle));
            }
            else if (_dynamicSubscriptions[_currentDynamicIndex].observable != observable)
            {
                // Replace existing subscription
                _dynamicSubscriptions[_currentDynamicIndex].handle.Dispose();
                var handle = observable.Subscribe(CreateScheduleCallback());
                _dynamicSubscriptions[_currentDynamicIndex] = (observable, handle);
            }
            
            _currentDynamicIndex++;
        }

        public void EndDynamicTracking()
        {
            // Dispose unused dynamic subscriptions
            while (_dynamicSubscriptions.Count > _currentDynamicIndex)
            {
                var lastIndex = _dynamicSubscriptions.Count - 1;
                _dynamicSubscriptions[lastIndex].handle.Dispose();
                _dynamicSubscriptions.RemoveAt(lastIndex);
            }
        }

        private bool ShouldTrack(IObservable observable)
        {
            return observable != _owner && _trackedDependencies.Add(observable);
        }

        private Action CreateScheduleCallback()
        {
            var owner = _owner; // Copy to local variable to avoid struct 'this' capture
            return () => owner.ScheduleExecution();
        }

        public void Dispose()
        {
            foreach (var (_, handle) in _staticSubscriptions)
            {
                handle?.Dispose();
            }
            
            foreach (var (_, handle) in _dynamicSubscriptions)
            {
                handle?.Dispose();
            }
            
            _staticSubscriptions.Clear();
            _dynamicSubscriptions.Clear();
            _trackedDependencies.Clear();
        }
    }
}