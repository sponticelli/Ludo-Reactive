using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents a collection that provides notifications when items get added, removed, or when the whole list is refreshed.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    [Serializable]
    public class ReactiveCollection<T> : IList<T>, INotifyCollectionChanged, IDisposable
    {
        [SerializeField] private List<T> _items;
        private Subject<CollectionAddEvent<T>> _addSubject;
        private Subject<CollectionRemoveEvent<T>> _removeSubject;
        private Subject<CollectionReplaceEvent<T>> _replaceSubject;
        private Subject<CollectionMoveEvent<T>> _moveSubject;
        private Subject<CollectionResetEvent<T>> _resetSubject;
        private Subject<int> _countSubject;
        private bool _disposed;

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Initializes a new instance of the ReactiveCollection class.
        /// </summary>
        public ReactiveCollection()
        {
            _items = new List<T>();
            InitializeSubjects();
        }

        /// <summary>
        /// Initializes a new instance of the ReactiveCollection class with the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the collection.</param>
        public ReactiveCollection(int capacity)
        {
            _items = new List<T>(capacity);
            InitializeSubjects();
        }

        /// <summary>
        /// Initializes a new instance of the ReactiveCollection class with the specified collection.
        /// </summary>
        /// <param name="collection">The collection to copy elements from.</param>
        public ReactiveCollection(IEnumerable<T> collection)
        {
            _items = new List<T>(collection);
            InitializeSubjects();
        }

        private void InitializeSubjects()
        {
            _addSubject = new Subject<CollectionAddEvent<T>>();
            _removeSubject = new Subject<CollectionRemoveEvent<T>>();
            _replaceSubject = new Subject<CollectionReplaceEvent<T>>();
            _moveSubject = new Subject<CollectionMoveEvent<T>>();
            _resetSubject = new Subject<CollectionResetEvent<T>>();
            _countSubject = new Subject<int>();
        }

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                CheckDisposed();
                return _items[index];
            }
            set
            {
                CheckDisposed();
                var oldValue = _items[index];
                _items[index] = value;
                
                var replaceEvent = new CollectionReplaceEvent<T>(index, oldValue, value);
                _replaceSubject.OnNext(replaceEvent);
                
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, value, oldValue, index));
            }
        }

        /// <summary>
        /// Observes when items are added to the collection.
        /// </summary>
        /// <returns>An observable sequence of add events.</returns>
        public IObservable<CollectionAddEvent<T>> ObserveAdd()
        {
            CheckDisposed();
            return _addSubject;
        }

        /// <summary>
        /// Observes when items are removed from the collection.
        /// </summary>
        /// <returns>An observable sequence of remove events.</returns>
        public IObservable<CollectionRemoveEvent<T>> ObserveRemove()
        {
            CheckDisposed();
            return _removeSubject;
        }

        /// <summary>
        /// Observes when items are replaced in the collection.
        /// </summary>
        /// <returns>An observable sequence of replace events.</returns>
        public IObservable<CollectionReplaceEvent<T>> ObserveReplace()
        {
            CheckDisposed();
            return _replaceSubject;
        }

        /// <summary>
        /// Observes when items are moved in the collection.
        /// </summary>
        /// <returns>An observable sequence of move events.</returns>
        public IObservable<CollectionMoveEvent<T>> ObserveMove()
        {
            CheckDisposed();
            return _moveSubject;
        }

        /// <summary>
        /// Observes when the collection is reset.
        /// </summary>
        /// <returns>An observable sequence of reset events.</returns>
        public IObservable<CollectionResetEvent<T>> ObserveReset()
        {
            CheckDisposed();
            return _resetSubject;
        }

        /// <summary>
        /// Observes changes to the collection count.
        /// </summary>
        /// <returns>An observable sequence of count changes.</returns>
        public IObservable<int> ObserveCountChanged()
        {
            CheckDisposed();
            return _countSubject.DistinctUntilChanged();
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            CheckDisposed();
            var index = _items.Count;
            _items.Add(item);
            
            var addEvent = new CollectionAddEvent<T>(index, item);
            _addSubject.OnNext(addEvent);
            _countSubject.OnNext(_items.Count);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, item, index));
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();
            var oldItems = new List<T>(_items);
            _items.Clear();
            
            var resetEvent = new CollectionResetEvent<T>(oldItems);
            _resetSubject.OnNext(resetEvent);
            _countSubject.OnNext(0);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the collection.</param>
        /// <returns>true if item is found in the collection; otherwise, false.</returns>
        public bool Contains(T item)
        {
            CheckDisposed();
            return _items.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from collection.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            CheckDisposed();
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            CheckDisposed();
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Determines the index of a specific item in the collection.
        /// </summary>
        /// <param name="item">The object to locate in the collection.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            CheckDisposed();
            return _items.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the collection.</param>
        public void Insert(int index, T item)
        {
            CheckDisposed();
            _items.Insert(index, item);
            
            var addEvent = new CollectionAddEvent<T>(index, item);
            _addSubject.OnNext(addEvent);
            _countSubject.OnNext(_items.Count);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, item, index));
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the collection.
        /// </summary>
        /// <param name="item">The object to remove from the collection.</param>
        /// <returns>true if item was successfully removed from the collection; otherwise, false.</returns>
        public bool Remove(T item)
        {
            CheckDisposed();
            var index = _items.IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            CheckDisposed();
            var item = _items[index];
            _items.RemoveAt(index);
            
            var removeEvent = new CollectionRemoveEvent<T>(index, item);
            _removeSubject.OnNext(removeEvent);
            _countSubject.OnNext(_items.Count);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, item, index));
        }

        /// <summary>
        /// Moves an item from one position to another.
        /// </summary>
        /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
        /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
        public void Move(int oldIndex, int newIndex)
        {
            CheckDisposed();
            if (oldIndex == newIndex) return;
            
            var item = _items[oldIndex];
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);
            
            var moveEvent = new CollectionMoveEvent<T>(oldIndex, newIndex, item);
            _moveSubject.OnNext(moveEvent);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ReactiveCollection<T>));
        }

        /// <summary>
        /// Releases all resources used by the ReactiveCollection.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _addSubject?.Dispose();
            _removeSubject?.Dispose();
            _replaceSubject?.Dispose();
            _moveSubject?.Dispose();
            _resetSubject?.Dispose();
            _countSubject?.Dispose();
            _items?.Clear();
        }
    }
}
