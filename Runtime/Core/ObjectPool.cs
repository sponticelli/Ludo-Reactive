using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// A thread-safe object pool for frequently created reactive instances to reduce garbage collection.
    /// Supports observable instance pooling, subscription object pooling, and event argument object pooling.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    public sealed class ObjectPool<T> : IDisposable where T : class
    {
        private readonly ConcurrentQueue<T> _objects;
        private readonly Func<T> _objectFactory;
        private readonly Action<T> _resetAction;
        private readonly int _maxSize;
        private int _currentCount;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ObjectPool class.
        /// </summary>
        /// <param name="objectFactory">Factory function to create new objects.</param>
        /// <param name="resetAction">Action to reset objects when returned to pool.</param>
        /// <param name="maxSize">Maximum number of objects to keep in the pool.</param>
        public ObjectPool(Func<T> objectFactory, Action<T> resetAction = null, int maxSize = 100)
        {
            _objectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));
            _resetAction = resetAction;
            _maxSize = maxSize > 0 ? maxSize : throw new ArgumentOutOfRangeException(nameof(maxSize));
            _objects = new ConcurrentQueue<T>();
            _currentCount = 0;
        }

        /// <summary>
        /// Gets the current number of objects in the pool.
        /// </summary>
        public int Count => _currentCount;

        /// <summary>
        /// Gets the maximum size of the pool.
        /// </summary>
        public int MaxSize => _maxSize;

        /// <summary>
        /// Gets an object from the pool or creates a new one if the pool is empty.
        /// </summary>
        /// <returns>An object from the pool.</returns>
        public T Get()
        {
            CheckDisposed();

            if (_objects.TryDequeue(out var obj))
            {
                System.Threading.Interlocked.Decrement(ref _currentCount);
                return obj;
            }

            return _objectFactory();
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="obj">The object to return to the pool.</param>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            CheckDisposed();

            if (_currentCount >= _maxSize)
                return; // Pool is full, discard the object

            try
            {
                _resetAction?.Invoke(obj);
                _objects.Enqueue(obj);
                System.Threading.Interlocked.Increment(ref _currentCount);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Error resetting object for pool: {ex}");
            }
        }

        /// <summary>
        /// Clears all objects from the pool.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();

            while (_objects.TryDequeue(out var obj))
            {
                if (obj is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Error disposing pooled object: {ex}");
                    }
                }
            }

            _currentCount = 0;
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
        }

        /// <summary>
        /// Releases all resources used by the ObjectPool.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Provides pooled instances for commonly used reactive objects.
    /// </summary>
    public static class ReactiveObjectPools
    {
        private static readonly ObjectPool<List<IDisposable>> _disposableListPool;
        private static readonly ObjectPool<List<object>> _objectListPool;
        private static readonly ObjectPool<Dictionary<Type, object>> _typeDictionaryPool;

        static ReactiveObjectPools()
        {
            _disposableListPool = new ObjectPool<List<IDisposable>>(
                () => new List<IDisposable>(),
                list => list.Clear(),
                50
            );

            _objectListPool = new ObjectPool<List<object>>(
                () => new List<object>(),
                list => list.Clear(),
                50
            );

            _typeDictionaryPool = new ObjectPool<Dictionary<Type, object>>(
                () => new Dictionary<Type, object>(),
                dict => dict.Clear(),
                20
            );
        }

        /// <summary>
        /// Gets a pooled List&lt;IDisposable&gt; instance.
        /// </summary>
        /// <returns>A pooled list instance.</returns>
        public static List<IDisposable> GetDisposableList()
        {
            return _disposableListPool.Get();
        }

        /// <summary>
        /// Returns a List&lt;IDisposable&gt; instance to the pool.
        /// </summary>
        /// <param name="list">The list to return to the pool.</param>
        public static void ReturnDisposableList(List<IDisposable> list)
        {
            _disposableListPool.Return(list);
        }

        /// <summary>
        /// Gets a pooled List&lt;object&gt; instance.
        /// </summary>
        /// <returns>A pooled list instance.</returns>
        public static List<object> GetObjectList()
        {
            return _objectListPool.Get();
        }

        /// <summary>
        /// Returns a List&lt;object&gt; instance to the pool.
        /// </summary>
        /// <param name="list">The list to return to the pool.</param>
        public static void ReturnObjectList(List<object> list)
        {
            _objectListPool.Return(list);
        }

        /// <summary>
        /// Gets a pooled Dictionary&lt;Type, object&gt; instance.
        /// </summary>
        /// <returns>A pooled dictionary instance.</returns>
        public static Dictionary<Type, object> GetTypeDictionary()
        {
            return _typeDictionaryPool.Get();
        }

        /// <summary>
        /// Returns a Dictionary&lt;Type, object&gt; instance to the pool.
        /// </summary>
        /// <param name="dictionary">The dictionary to return to the pool.</param>
        public static void ReturnTypeDictionary(Dictionary<Type, object> dictionary)
        {
            _typeDictionaryPool.Return(dictionary);
        }

        /// <summary>
        /// Clears all object pools.
        /// </summary>
        public static void ClearAll()
        {
            _disposableListPool.Clear();
            _objectListPool.Clear();
            _typeDictionaryPool.Clear();
        }
    }

    /// <summary>
    /// Provides extension methods for working with object pools.
    /// </summary>
    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// Creates a disposable that returns an object to the pool when disposed.
        /// </summary>
        /// <typeparam name="T">The type of the pooled object.</typeparam>
        /// <param name="pool">The object pool.</param>
        /// <param name="obj">The object to return to the pool.</param>
        /// <returns>A disposable that returns the object to the pool.</returns>
        public static IDisposable CreateReturnDisposable<T>(this ObjectPool<T> pool, T obj) where T : class
        {
            return Disposable.Create(() => pool.Return(obj));
        }

        /// <summary>
        /// Gets an object from the pool and creates a disposable that returns it when disposed.
        /// </summary>
        /// <typeparam name="T">The type of the pooled object.</typeparam>
        /// <param name="pool">The object pool.</param>
        /// <param name="obj">The object retrieved from the pool.</param>
        /// <returns>A disposable that returns the object to the pool.</returns>
        public static IDisposable GetWithAutoReturn<T>(this ObjectPool<T> pool, out T obj) where T : class
        {
            obj = pool.Get();
            return pool.CreateReturnDisposable(obj);
        }
    }

    /// <summary>
    /// Provides extension methods for ReactiveObjectPools.
    /// </summary>
    public static class ReactiveObjectPoolsExtensions
    {
        /// <summary>
        /// Gets a pooled List&lt;object&gt; and creates a disposable that returns it when disposed.
        /// </summary>
        /// <param name="list">The list retrieved from the pool.</param>
        /// <returns>A disposable that returns the list to the pool.</returns>
        public static IDisposable GetWithAutoReturn(out List<object> list)
        {
            var pooledList = ReactiveObjectPools.GetObjectList();
            list = pooledList;
            return Disposable.Create(() => ReactiveObjectPools.ReturnObjectList(pooledList));
        }
    }

    /// <summary>
    /// A pooled disposable that can be reused to reduce garbage collection.
    /// </summary>
    public sealed class PooledDisposable : IDisposable
    {
        private static readonly ObjectPool<PooledDisposable> _pool = new ObjectPool<PooledDisposable>(
            () => new PooledDisposable(),
            disposable => disposable.Reset(),
            100
        );

        private Action _disposeAction;
        private bool _disposed;

        private PooledDisposable()
        {
        }

        /// <summary>
        /// Creates a pooled disposable with the specified dispose action.
        /// </summary>
        /// <param name="disposeAction">The action to execute when disposed.</param>
        /// <returns>A pooled disposable instance.</returns>
        public static PooledDisposable Create(Action disposeAction)
        {
            var disposable = _pool.Get();
            disposable._disposeAction = disposeAction;
            disposable._disposed = false;
            return disposable;
        }

        /// <summary>
        /// Executes the dispose action and returns this instance to the pool.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _disposeAction?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] PooledDisposable action threw exception: {ex}");
            }
            finally
            {
                _pool.Return(this);
            }
        }

        private void Reset()
        {
            _disposeAction = null;
            _disposed = false;
        }
    }
}
