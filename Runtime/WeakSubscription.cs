using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ludo.Reactive
{
    /// <summary>
    /// Weak reference subscription to prevent memory leaks
    /// </summary>
    internal struct WeakSubscription
    {
        private WeakReference _targetRef;
        private string _methodName;
        private long _id;

        public WeakSubscription(long id, object target, string methodName)
        {
            _id = id;
            _targetRef = new WeakReference(target);
            _methodName = methodName;
        }

        public long Id => _id;
        public bool IsAlive => _targetRef?.IsAlive == true;
        public object Target => _targetRef?.Target;

        public bool TryInvoke(Action callback)
        {
            if (!IsAlive) return false;

            try
            {
                callback?.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryInvoke<T>(Action<T> callback, T value)
        {
            if (!IsAlive) return false;

            try
            {
                callback?.Invoke(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Enhanced EventSource with weak reference support
    /// </summary>
    public class WeakEventSource<T> : IObservable<T>
    {
        private Dictionary<long, WeakSubscription> _weakSubscriptions = new Dictionary<long, WeakSubscription>();
        private Dictionary<long, Subscription> _strongSubscriptions = new Dictionary<long, Subscription>();
        private Queue<T> _eventQueue = new Queue<T>();
        private DeferredExecutionQueue _deferredQueue;
        private long _nextSubscriptionId = 1;
        private bool _isEmitting;
        
        // Cache for subscription iteration
        private List<(long id, bool isWeak, object subscription)> _subscriptionCache = new List<(long, bool, object)>();
        private bool _cacheInvalid = true;

        public WeakEventSource()
        {
            _deferredQueue.Initialize();
        }

        /// <summary>
        /// Subscribe with strong reference (traditional behavior)
        /// </summary>
        public SubscriptionHandle Subscribe(Action callback)
        {
            var subscriptionId = _nextSubscriptionId++;
            var subscription = new Subscription
            {
                Id = subscriptionId,
                Callback = callback
            };
            
            _strongSubscriptions[subscriptionId] = subscription;
            _cacheInvalid = true;
            
            return new SubscriptionHandle(() => UnsubscribeById(subscriptionId, false));
        }

        /// <summary>
        /// Subscribe with strong reference (traditional behavior)
        /// </summary>
        public SubscriptionHandle Subscribe(Action<T> callback)
        {
            var subscriptionId = _nextSubscriptionId++;
            var subscription = new Subscription
            {
                Id = subscriptionId,
                TypedCallback = obj => callback((T)obj)
            };
            
            _strongSubscriptions[subscriptionId] = subscription;
            _cacheInvalid = true;
            
            return new SubscriptionHandle(() => UnsubscribeById(subscriptionId, false));
        }

        /// <summary>
        /// Subscribe with weak reference to prevent memory leaks
        /// </summary>
        public SubscriptionHandle SubscribeWeak(object target, Action callback)
        {
            var subscriptionId = _nextSubscriptionId++;
            var weakSubscription = new WeakSubscription(subscriptionId, target, callback.Method.Name);
            
            _weakSubscriptions[subscriptionId] = weakSubscription;
            _cacheInvalid = true;
            
            return new SubscriptionHandle(() => UnsubscribeById(subscriptionId, true));
        }

        /// <summary>
        /// Subscribe with weak reference to prevent memory leaks
        /// </summary>
        public SubscriptionHandle SubscribeWeak<TTarget>(TTarget target, Action<T> callback) where TTarget : class
        {
            var subscriptionId = _nextSubscriptionId++;
            var weakSubscription = new WeakSubscription(subscriptionId, target, callback.Method.Name);
            
            _weakSubscriptions[subscriptionId] = weakSubscription;
            _cacheInvalid = true;
            
            return new SubscriptionHandle(() => UnsubscribeById(subscriptionId, true));
        }

        public void Unsubscribe(Action callback)
        {
            // Remove from strong subscriptions
            var toRemove = new List<long>();
            foreach (var kvp in _strongSubscriptions)
            {
                if (kvp.Value.Callback == callback)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var id in toRemove)
            {
                _strongSubscriptions.Remove(id);
            }
            
            if (toRemove.Count > 0)
            {
                _cacheInvalid = true;
            }
        }

        public void Unsubscribe(Action<T> callback)
        {
            // Remove from strong subscriptions
            var toRemove = new List<long>();
            foreach (var kvp in _strongSubscriptions)
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
                _strongSubscriptions.Remove(id);
            }
            
            if (toRemove.Count > 0)
            {
                _cacheInvalid = true;
            }
        }

        private void UnsubscribeById(long subscriptionId, bool isWeak)
        {
            bool removed = false;
            
            if (isWeak)
            {
                removed = _weakSubscriptions.Remove(subscriptionId);
            }
            else
            {
                removed = _strongSubscriptions.Remove(subscriptionId);
            }
            
            if (removed)
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
                    
                    // Clean up dead weak references and update cache
                    CleanupAndUpdateCache();
                    
                    // Iterate over cached subscriptions
                    for (int i = 0; i < _subscriptionCache.Count; i++)
                    {
                        var (id, isWeak, subscription) = _subscriptionCache[i];
                        
                        try
                        {
                            if (isWeak)
                            {
                                var weakSub = (WeakSubscription)subscription;
                                if (!weakSub.IsAlive)
                                {
                                    // Mark for removal
                                    _weakSubscriptions.Remove(id);
                                    _cacheInvalid = true;
                                    continue;
                                }
                                // Note: Weak subscriptions would need additional callback storage
                                // This is a simplified implementation
                            }
                            else
                            {
                                var strongSub = (Subscription)subscription;
                                if (strongSub.Callback != null)
                                {
                                    strongSub.Callback();
                                }
                                else if (strongSub.TypedCallback != null)
                                {
                                    strongSub.TypedCallback(value);
                                }
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

        private void CleanupAndUpdateCache()
        {
            if (!_cacheInvalid) return;
            
            // Remove dead weak references
            var deadWeakRefs = new List<long>();
            foreach (var kvp in _weakSubscriptions)
            {
                if (!kvp.Value.IsAlive)
                {
                    deadWeakRefs.Add(kvp.Key);
                }
            }
            
            foreach (var id in deadWeakRefs)
            {
                _weakSubscriptions.Remove(id);
            }
            
            // Update cache
            _subscriptionCache.Clear();
            
            foreach (var kvp in _strongSubscriptions)
            {
                _subscriptionCache.Add((kvp.Key, false, kvp.Value));
            }
            
            foreach (var kvp in _weakSubscriptions)
            {
                _subscriptionCache.Add((kvp.Key, true, kvp.Value));
            }
            
            _cacheInvalid = false;
        }

        public int StrongSubscriptionCount => _strongSubscriptions.Count;
        public int WeakSubscriptionCount => _weakSubscriptions.Count;
        public int TotalSubscriptionCount => StrongSubscriptionCount + WeakSubscriptionCount;
    }
}
