using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Object pool for reusing objects
    /// </summary>
    public struct ObjectPool<T> where T : new()
    {
        private Stack<T> _availableObjects;
        private Action<T> _resetAction;

        public static ObjectPool<T> Create(Action<T> resetAction = null)
        {
            return new ObjectPool<T>
            {
                _availableObjects = new Stack<T>(),
                _resetAction = resetAction
            };
        }

        public T Rent()
        {
            return _availableObjects?.Count > 0 ? _availableObjects.Pop() : new T();
        }

        public void Return(T obj)
        {
            _resetAction?.Invoke(obj);
            _availableObjects?.Push(obj);
        }
    }
}