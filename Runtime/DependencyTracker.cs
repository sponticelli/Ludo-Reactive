using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Tracks static and dynamic dependencies for computations with dirty checking optimization
    /// </summary>
    public class DependencyTracker : IDisposable
    {
        private ReactiveComputation _owner;
        private HashSet<IObservable> _trackedDependencies;
        private List<(IObservable observable, SubscriptionHandle handle)> _staticSubscriptions;
        private List<(IObservable observable, SubscriptionHandle handle)> _dynamicSubscriptions;
        private int _currentDynamicIndex;

        // Dirty checking optimization
        private Dictionary<IObservable, object> _lastKnownValues;
        private bool _hasDirtyDependencies;

        public DependencyTracker(ReactiveComputation owner)
        {
            _owner = owner;
            _trackedDependencies = new HashSet<IObservable>();
            _staticSubscriptions = new List<(IObservable, SubscriptionHandle)>();
            _dynamicSubscriptions = new List<(IObservable, SubscriptionHandle)>();
            _currentDynamicIndex = 0;
            _lastKnownValues = new Dictionary<IObservable, object>();
            _hasDirtyDependencies = true; // Start dirty to ensure first execution
        }

        public void AddStaticDependency(IObservable observable)
        {
            if (ShouldTrack(observable))
            {
                var handle = observable.Subscribe(CreateScheduleCallback(observable));
                _staticSubscriptions.Add((observable, handle));
                _trackedDependencies.Add(observable);

                // Initialize last known value for dirty checking
                // Note: This is a simplified approach. In practice, you'd need to handle different generic types
                try
                {
                    var currentProperty = observable.GetType().GetProperty("Current");
                    if (currentProperty != null)
                    {
                        _lastKnownValues[observable] = currentProperty.GetValue(observable);
                    }
                }
                catch
                {
                    // Ignore if we can't get the current value
                }
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
                var handle = observable.Subscribe(CreateScheduleCallback(observable));
                _dynamicSubscriptions.Add((observable, handle));

                // Initialize last known value for dirty checking
                try
                {
                    var currentProperty = observable.GetType().GetProperty("Current");
                    if (currentProperty != null)
                    {
                        _lastKnownValues[observable] = currentProperty.GetValue(observable);
                    }
                }
                catch
                {
                    // Ignore if we can't get the current value
                }
            }
            else if (_dynamicSubscriptions[_currentDynamicIndex].observable != observable)
            {
                // Replace existing subscription
                _dynamicSubscriptions[_currentDynamicIndex].handle.Dispose();
                var handle = observable.Subscribe(CreateScheduleCallback(observable));
                _dynamicSubscriptions[_currentDynamicIndex] = (observable, handle);

                // Update last known value for dirty checking
                try
                {
                    var currentProperty = observable.GetType().GetProperty("Current");
                    if (currentProperty != null)
                    {
                        _lastKnownValues[observable] = currentProperty.GetValue(observable);
                    }
                }
                catch
                {
                    // Ignore if we can't get the current value
                }
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

        private Action CreateScheduleCallback(IObservable observable)
        {
            var owner = _owner; // Copy to local variable
            return () =>
            {
                // Mark as dirty when dependency changes
                _hasDirtyDependencies = true;
                owner.ScheduleExecution();
            };
        }

        /// <summary>
        /// Checks if any dependencies have changed since last execution
        /// </summary>
        public bool HasDirtyDependencies()
        {
            if (_hasDirtyDependencies) return true;

            // Check if any tracked values have changed
            foreach (var kvp in _lastKnownValues)
            {
                try
                {
                    var currentProperty = kvp.Key.GetType().GetProperty("Current");
                    if (currentProperty != null)
                    {
                        var currentValue = currentProperty.GetValue(kvp.Key);
                        if (!Equals(kvp.Value, currentValue))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // If we can't get the current value, assume it's dirty
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the last known values after computation execution
        /// </summary>
        public void UpdateLastKnownValues()
        {
            foreach (var observable in _trackedDependencies)
            {
                try
                {
                    var currentProperty = observable.GetType().GetProperty("Current");
                    if (currentProperty != null)
                    {
                        _lastKnownValues[observable] = currentProperty.GetValue(observable);
                    }
                }
                catch
                {
                    // Ignore if we can't get the current value
                }
            }
            _hasDirtyDependencies = false;
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