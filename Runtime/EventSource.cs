using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludo.Reactive
{
    /// <summary>
    /// Event source implementation for observables
    /// </summary>
    public class EventSource<T> : IObservable<T>
    {
        private List<Subscription> _subscriptions = new List<Subscription>();
        private Queue<T> _eventQueue = new Queue<T>();
        private DeferredExecutionQueue _deferredQueue;
        private long _nextSubscriptionId = 1;
        private bool _isEmitting;

        public EventSource()
        {
            _deferredQueue.Initialize();
        }

        public SubscriptionHandle Subscribe(Action callback)
        {
            var subscription = new Subscription
            {
                Id = _nextSubscriptionId++,
                Callback = callback
            };
            
            _subscriptions.Add(subscription);
            
            return new SubscriptionHandle(() => Unsubscribe(callback));
        }

        public SubscriptionHandle Subscribe(Action<T> callback)
        {
            var subscription = new Subscription
            {
                Id = _nextSubscriptionId++,
                TypedCallback = obj => callback((T)obj)
            };
            
            _subscriptions.Add(subscription);
            
            return new SubscriptionHandle(() => Unsubscribe(callback));
        }

        public void Unsubscribe(Action callback)
        {
            _subscriptions.RemoveAll(s => s.Callback == callback);
        }

        public void Unsubscribe(Action<T> callback)
        {
            _subscriptions.RemoveAll(s => s.TypedCallback != null && 
                                          s.TypedCallback.Method == callback.Method && 
                                          s.TypedCallback.Target == callback.Target);
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
                    
                    // Create a copy of subscriptions to avoid modification during iteration
                    var currentSubscriptions = _subscriptions.ToList();
                    
                    foreach (var subscription in currentSubscriptions)
                    {
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
    }
}