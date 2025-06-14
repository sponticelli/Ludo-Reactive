using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

namespace Ludo.Reactive.Collections
{
    /// <summary>
    /// An observable list that provides granular change tracking and notifications.
    /// Extends the basic ReactiveCollection with enhanced performance and features.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    [Serializable]
    public class ObservableList<T> : IList<T>, INotifyCollectionChanged, IObservable<CollectionChangeSet<T>>, IDisposable
    {
        [SerializeField] private List<T> _items;
        private Subject<CollectionChangeSet<T>> _changeSetSubject;
        private Subject<CollectionAddEvent<T>> _addSubject;
        private Subject<CollectionRemoveEvent<T>> _removeSubject;
        private Subject<CollectionReplaceEvent<T>> _replaceSubject;
        private Subject<CollectionMoveEvent<T>> _moveSubject;
        private Subject<CollectionResetEvent<T>> _resetSubject;
        private Subject<int> _countSubject;
        private readonly IEqualityComparer<T> _comparer;
        private bool _disposed;

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Gets the number of elements in the list.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Gets a value indicating whether the list is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets an observable stream of add events.
        /// </summary>
        public IObservable<CollectionAddEvent<T>> ObserveAdd() => _addSubject;

        /// <summary>
        /// Gets an observable stream of remove events.
        /// </summary>
        public IObservable<CollectionRemoveEvent<T>> ObserveRemove() => _removeSubject;

        /// <summary>
        /// Gets an observable stream of replace events.
        /// </summary>
        public IObservable<CollectionReplaceEvent<T>> ObserveReplace() => _replaceSubject;

        /// <summary>
        /// Gets an observable stream of move events.
        /// </summary>
        public IObservable<CollectionMoveEvent<T>> ObserveMove() => _moveSubject;

        /// <summary>
        /// Gets an observable stream of reset events.
        /// </summary>
        public IObservable<CollectionResetEvent<T>> ObserveReset() => _resetSubject;

        /// <summary>
        /// Gets an observable stream of count changes.
        /// </summary>
        public IObservable<int> ObserveCountChanged() => _countSubject;

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
                if (!_comparer.Equals(oldValue, value))
                {
                    _items[index] = value;
                    
                    var replaceEvent = new CollectionReplaceEvent<T>(index, oldValue, value);
                    _replaceSubject.OnNext(replaceEvent);
                    
                    var changeSet = new CollectionChangeSet<T>(new[] { new CollectionChange<T>(CollectionChangeType.Replace, index, oldValue, value) });
                    _changeSetSubject.OnNext(changeSet);
                    
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace, value, oldValue, index));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the ObservableList class.
        /// </summary>
        /// <param name="comparer">The equality comparer to use for comparing elements.</param>
        public ObservableList(IEqualityComparer<T> comparer = null)
        {
            _items = new List<T>();
            _comparer = comparer ?? EqualityComparer<T>.Default;
            InitializeSubjects();
        }

        /// <summary>
        /// Initializes a new instance of the ObservableList class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the list.</param>
        /// <param name="comparer">The equality comparer to use for comparing elements.</param>
        public ObservableList(int capacity, IEqualityComparer<T> comparer = null)
        {
            _items = new List<T>(capacity);
            _comparer = comparer ?? EqualityComparer<T>.Default;
            InitializeSubjects();
        }

        /// <summary>
        /// Initializes a new instance of the ObservableList class with the specified collection.
        /// </summary>
        /// <param name="collection">The collection to copy elements from.</param>
        /// <param name="comparer">The equality comparer to use for comparing elements.</param>
        public ObservableList(IEnumerable<T> collection, IEqualityComparer<T> comparer = null)
        {
            _items = new List<T>(collection);
            _comparer = comparer ?? EqualityComparer<T>.Default;
            InitializeSubjects();
        }

        private void InitializeSubjects()
        {
            _changeSetSubject = new Subject<CollectionChangeSet<T>>();
            _addSubject = new Subject<CollectionAddEvent<T>>();
            _removeSubject = new Subject<CollectionRemoveEvent<T>>();
            _replaceSubject = new Subject<CollectionReplaceEvent<T>>();
            _moveSubject = new Subject<CollectionMoveEvent<T>>();
            _resetSubject = new Subject<CollectionResetEvent<T>>();
            _countSubject = new Subject<int>();
        }

        /// <summary>
        /// Adds an item to the list.
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
            
            var changeSet = new CollectionChangeSet<T>(new[] { new CollectionChange<T>(CollectionChangeType.Add, index, default, item) });
            _changeSetSubject.OnNext(changeSet);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, item, index));
        }

        /// <summary>
        /// Adds multiple items to the list in a single operation.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            CheckDisposed();

            var itemsArray = items.ToArray();
            if (itemsArray.Length == 0)
                return;

            var startIndex = _items.Count;
            _items.AddRange(itemsArray);

            var changes = new List<CollectionChange<T>>();
            for (int i = 0; i < itemsArray.Length; i++)
            {
                var index = startIndex + i;
                var item = itemsArray[i];
                
                var addEvent = new CollectionAddEvent<T>(index, item);
                _addSubject.OnNext(addEvent);
                
                changes.Add(new CollectionChange<T>(CollectionChangeType.Add, index, default, item));
            }

            _countSubject.OnNext(_items.Count);
            
            var changeSet = new CollectionChangeSet<T>(changes);
            _changeSetSubject.OnNext(changeSet);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, itemsArray.ToList(), startIndex));
        }

        /// <summary>
        /// Removes all items from the list.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();
            if (_items.Count == 0)
                return;

            var oldItems = _items.ToArray();
            _items.Clear();
            
            var resetEvent = new CollectionResetEvent<T>(oldItems);
            _resetSubject.OnNext(resetEvent);
            _countSubject.OnNext(0);
            
            var changes = oldItems.Select((item, index) => 
                new CollectionChange<T>(CollectionChangeType.Remove, index, item, default)).ToArray();
            var changeSet = new CollectionChangeSet<T>(changes);
            _changeSetSubject.OnNext(changeSet);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Determines whether the list contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns>True if item is found in the list; otherwise, false.</returns>
        public bool Contains(T item)
        {
            CheckDisposed();
            return _items.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the list to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from list.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            CheckDisposed();
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the list.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            CheckDisposed();
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Determines the index of a specific item in the list.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            CheckDisposed();
            return _items.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the list at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the list.</param>
        public void Insert(int index, T item)
        {
            CheckDisposed();
            _items.Insert(index, item);
            
            var addEvent = new CollectionAddEvent<T>(index, item);
            _addSubject.OnNext(addEvent);
            _countSubject.OnNext(_items.Count);
            
            var changeSet = new CollectionChangeSet<T>(new[] { new CollectionChange<T>(CollectionChangeType.Add, index, default, item) });
            _changeSetSubject.OnNext(changeSet);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, item, index));
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the list.
        /// </summary>
        /// <param name="item">The object to remove from the list.</param>
        /// <returns>True if item was successfully removed from the list; otherwise, false.</returns>
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
            
            var changeSet = new CollectionChangeSet<T>(new[] { new CollectionChange<T>(CollectionChangeType.Remove, index, item, default) });
            _changeSetSubject.OnNext(changeSet);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, item, index));
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<CollectionChangeSet<T>> observer)
        {
            CheckDisposed();
            return _changeSetSubject.Subscribe(observer);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ObservableList<T>));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            _changeSetSubject?.Dispose();
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
