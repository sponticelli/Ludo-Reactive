using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Object pool for reusing objects with enhanced pooling capabilities
    /// </summary>
    public struct ObjectPool<T> where T : new()
    {
        private Stack<T> _availableObjects;
        private Action<T> _resetAction;
        private Func<T> _createAction;
        private int _maxSize;

        public static ObjectPool<T> Create(Action<T> resetAction = null, int maxSize = 100)
        {
            return new ObjectPool<T>
            {
                _availableObjects = new Stack<T>(),
                _resetAction = resetAction,
                _createAction = () => new T(),
                _maxSize = maxSize
            };
        }

        public static ObjectPool<T> Create(Func<T> createAction, Action<T> resetAction = null, int maxSize = 100)
        {
            return new ObjectPool<T>
            {
                _availableObjects = new Stack<T>(),
                _resetAction = resetAction,
                _createAction = createAction ?? (() => new T()),
                _maxSize = maxSize
            };
        }

        public T Rent()
        {
            if (_availableObjects?.Count > 0)
            {
                return _availableObjects.Pop();
            }

            return _createAction();
        }

        public void Return(T obj)
        {
            if (obj == null) return;

            _resetAction?.Invoke(obj);

            // Only return to pool if we haven't exceeded max size
            if (_availableObjects != null && _availableObjects.Count < _maxSize)
            {
                _availableObjects.Push(obj);
            }
        }

        public int AvailableCount => _availableObjects?.Count ?? 0;
        public int MaxSize => _maxSize;
    }
}