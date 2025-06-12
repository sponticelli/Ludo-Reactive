using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludo.Reactive
{
    /// <summary>
    /// Event source implementation for observables with optimized subscription management
    /// </summary>
    public class EventSource<T> : IObservable<T>
    {
        private Dictionary<long, Subscription> _subscriptions = new Dictionary<long, Subscription>();
        private Queue<T> _eventQueue = new Queue<T>();
        private DeferredExecutionQueue _deferredQueue;
        private long _nextSubscriptionId = 1;
        private bool _isEmitting;

        // Cache for subscription iteration to avoid dictionary enumeration overhead
        private List<Subscription> _subscriptionCache = new List<Subscription>();
        private bool _cacheInvalid = true;

        public EventSource()
        {
            _deferredQueue.Initialize();
        }

        public SubscriptionHandle Subscribe(Action callback)
        {
            var subscriptionId = _nextSubscriptionId++;
            var subscription = new Subscription
            {
                Id = subscriptionId,
                Callback = callback
            };

            _subscriptions[subscriptionId] = subscription;
            _cacheInvalid = true;

            return new SubscriptionHandle(() => UnsubscribeById(subscriptionId));
        }

        public SubscriptionHandle Subscribe(Action<T> callback)
        {
            var subscriptionId = _nextSubscriptionId++;
            var subscription = new Subscription
            {
                Id = subscriptionId,
                TypedCallback = obj => callback((T)obj)
            };

            _subscriptions[subscriptionId] = subscription;
            _cacheInvalid = true;

            return new SubscriptionHandle(() => UnsubscribeById(subscriptionId));
        }

        public void Unsubscribe(Action callback)
        {
            // Linear search for backward compatibility - consider deprecating this method
            var toRemove = new List<long>();
            foreach (var kvp in _subscriptions)
            {
                if (kvp.Value.Callback == callback)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                _subscriptions.Remove(id);
            }

            if (toRemove.Count > 0)
            {
                _cacheInvalid = true;
            }
        }

        public void Unsubscribe(Action<T> callback)
        {
            // Linear search for backward compatibility - consider deprecating this method
            var toRemove = new List<long>();
            foreach (var kvp in _subscriptions)
            {
                if (kvp.Value.TypedCallback != null &&
                    kvp.Value.TypedCallback.Method == callback.Method &&
                    kvp.Value.TypedCallback.Target == callback.Target)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                _subscriptions.Remove(id);
            }

            if (toRemove.Count > 0)
            {
                _cacheInvalid = true;
            }
        }

        private void UnsubscribeById(long subscriptionId)
        {
            if (_subscriptions.Remove(subscriptionId))
            {
                _cacheInvalid = true;
            }
        }

        public void Emit(T value)
        {
            _eventQueue.Enqueue(value);
            _deferredQueue.Schedule(FlushEvents);
        }

        private void FlushEvents()
        {
            if (_isEmitting || _eventQueue.Count == 0) return;

            _isEmitting = true;
            try
            {
                while (_eventQueue.Count > 0)
                {
                    var value = _eventQueue.Dequeue();

                    // Update cache if needed to avoid ToList() allocation
                    UpdateSubscriptionCache();

                    // Iterate over cached subscriptions to avoid dictionary enumeration overhead
                    for (int i = 0; i < _subscriptionCache.Count; i++)
                    {
                        var subscription = _subscriptionCache[i];
                        try
                        {
                            if (subscription.Callback != null)
                            {
                                subscription.Callback();
                            }
                            else if (subscription.TypedCallback != null)
                            {
                                subscription.TypedCallback(value);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception in subscription callback: {ex}");
                        }
                    }
                }
            }
            finally
            {
                _isEmitting = false;
            }
        }

        private void UpdateSubscriptionCache()
        {
            if (!_cacheInvalid) return;

            _subscriptionCache.Clear();
            foreach (var subscription in _subscriptions.Values)
            {
                _subscriptionCache.Add(subscription);
            }
            _cacheInvalid = false;
        }
    }
}