using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ludo.Reactive.WebGL
{
    /// <summary>
    /// Provides WebGL-specific performance optimizations for reactive operations.
    /// Includes reduced garbage collection pressure, optimized coroutine usage,
    /// and memory-efficient observable chains for browser environments.
    /// </summary>
    public static class WebGLOptimizations
    {
        private static readonly Dictionary<Type, object> _cachedObservables = new Dictionary<Type, object>();
        private static readonly Queue<IEnumerator> _coroutinePool = new Queue<IEnumerator>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Creates a WebGL-optimized observable that minimizes garbage collection.
        /// </summary>
        /// <typeparam name="T">The type of elements in the observable sequence.</typeparam>
        /// <param name="subscribe">The subscribe function.</param>
        /// <returns>An optimized observable for WebGL.</returns>
        public static IObservable<T> CreateOptimized<T>(Func<IObserver<T>, IDisposable> subscribe)
        {
            if (subscribe == null)
                throw new ArgumentNullException(nameof(subscribe));

            return new WebGLOptimizedObservable<T>(subscribe);
        }

        /// <summary>
        /// Creates a cached observable that reuses instances to reduce memory allocation.
        /// </summary>
        /// <typeparam name="T">The type of elements in the observable sequence.</typeparam>
        /// <param name="factory">Factory function to create the observable.</param>
        /// <returns>A cached observable instance.</returns>
        public static IObservable<T> CreateCached<T>(Func<IObservable<T>> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var key = typeof(T);
            
            lock (_lock)
            {
                if (_cachedObservables.TryGetValue(key, out var cached))
                {
                    return (IObservable<T>)cached;
                }

                var observable = factory();
                _cachedObservables[key] = observable;
                return observable;
            }
        }

        /// <summary>
        /// Creates a WebGL-optimized interval observable that uses efficient timing.
        /// </summary>
        /// <param name="period">The period between emissions.</param>
        /// <returns>An optimized interval observable.</returns>
        public static IObservable<long> IntervalOptimized(TimeSpan period)
        {
            return CreateOptimized<long>(observer =>
            {
                var coroutine = IntervalCoroutine(period, observer);
                var runner = CoroutineRunner.Instance;
                runner.StartCoroutine(coroutine);

                return Disposable.Create(() =>
                {
                    runner.StopCoroutine(coroutine);
                    ReturnCoroutineToPool(coroutine);
                });
            });
        }

        /// <summary>
        /// Creates a WebGL-optimized timer observable.
        /// </summary>
        /// <param name="dueTime">The time to wait before emitting.</param>
        /// <returns>An optimized timer observable.</returns>
        public static IObservable<long> TimerOptimized(TimeSpan dueTime)
        {
            return CreateOptimized<long>(observer =>
            {
                var coroutine = TimerCoroutine(dueTime, observer);
                var runner = CoroutineRunner.Instance;
                runner.StartCoroutine(coroutine);

                return Disposable.Create(() =>
                {
                    runner.StopCoroutine(coroutine);
                    ReturnCoroutineToPool(coroutine);
                });
            });
        }

        /// <summary>
        /// Optimizes an observable chain for WebGL by reducing intermediate allocations.
        /// </summary>
        /// <typeparam name="T">The type of elements in the observable sequence.</typeparam>
        /// <param name="source">The source observable.</param>
        /// <returns>An optimized observable chain.</returns>
        public static IObservable<T> OptimizeForWebGL<T>(this IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return CreateOptimized<T>(observer =>
            {
                var subscription = source.Subscribe(
                    value =>
                    {
                        // Use try-catch to prevent exceptions from breaking the chain
                        try
                        {
                            observer.OnNext(value);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                );

                return subscription;
            });
        }

        /// <summary>
        /// Creates a batched observable that reduces the frequency of emissions for WebGL.
        /// </summary>
        /// <typeparam name="T">The type of elements in the observable sequence.</typeparam>
        /// <param name="source">The source observable.</param>
        /// <param name="batchSize">The maximum number of items per batch.</param>
        /// <param name="timeSpan">The maximum time to wait for a batch.</param>
        /// <returns>A batched observable optimized for WebGL.</returns>
        public static IObservable<IList<T>> BatchForWebGL<T>(this IObservable<T> source, int batchSize, TimeSpan timeSpan)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize));

            return CreateOptimized<IList<T>>(observer =>
            {
                var batch = ReactiveObjectPools.GetObjectList();
                var lastEmission = DateTime.UtcNow;
                var disposed = false;

                var subscription = source.Subscribe(
                    value =>
                    {
                        if (disposed) return;

                        batch.Add(value);

                        var shouldEmit = batch.Count >= batchSize || 
                                       (DateTime.UtcNow - lastEmission) >= timeSpan;

                        if (shouldEmit && batch.Count > 0)
                        {
                            var batchToEmit = new List<T>(batch.Cast<T>());
                            batch.Clear();
                            lastEmission = DateTime.UtcNow;
                            observer.OnNext(batchToEmit);
                        }
                    },
                    error =>
                    {
                        disposed = true;
                        ReactiveObjectPools.ReturnObjectList(batch);
                        observer.OnError(error);
                    },
                    () =>
                    {
                        disposed = true;
                        if (batch.Count > 0)
                        {
                            var finalBatch = new List<T>(batch.Cast<T>());
                            observer.OnNext(finalBatch);
                        }
                        ReactiveObjectPools.ReturnObjectList(batch);
                        observer.OnCompleted();
                    }
                );

                return Disposable.Create(() =>
                {
                    disposed = true;
                    subscription.Dispose();
                    ReactiveObjectPools.ReturnObjectList(batch);
                });
            });
        }

        private static IEnumerator IntervalCoroutine(TimeSpan period, IObserver<long> observer)
        {
            var count = 0L;
            var periodSeconds = (float)period.TotalSeconds;

            while (true)
            {
                yield return new WaitForSeconds(periodSeconds);
                
                try
                {
                    observer.OnNext(count++);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                    yield break;
                }
            }
        }

        private static IEnumerator TimerCoroutine(TimeSpan dueTime, IObserver<long> observer)
        {
            var dueTimeSeconds = (float)dueTime.TotalSeconds;
            yield return new WaitForSeconds(dueTimeSeconds);

            try
            {
                observer.OnNext(0L);
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        private static void ReturnCoroutineToPool(IEnumerator coroutine)
        {
            lock (_lock)
            {
                if (_coroutinePool.Count < 10) // Limit pool size
                {
                    _coroutinePool.Enqueue(coroutine);
                }
            }
        }

        /// <summary>
        /// Clears all cached observables and pools.
        /// </summary>
        public static void ClearCaches()
        {
            lock (_lock)
            {
                _cachedObservables.Clear();
                _coroutinePool.Clear();
            }
        }
    }

    /// <summary>
    /// A WebGL-optimized observable implementation that minimizes garbage collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the observable sequence.</typeparam>
    internal sealed class WebGLOptimizedObservable<T> : IObservable<T>
    {
        private readonly Func<IObserver<T>, IDisposable> _subscribe;

        public WebGLOptimizedObservable(Func<IObserver<T>, IDisposable> subscribe)
        {
            _subscribe = subscribe ?? throw new ArgumentNullException(nameof(subscribe));
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            try
            {
                return _subscribe(observer) ?? Disposable.Empty;
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
                return Disposable.Empty;
            }
        }
    }

    /// <summary>
    /// A singleton coroutine runner for WebGL optimizations.
    /// </summary>
    internal sealed class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        private static readonly object _instanceLock = new object();

        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("[Ludo.Reactive] CoroutineRunner");
                            _instance = go.AddComponent<CoroutineRunner>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }
    }
}
