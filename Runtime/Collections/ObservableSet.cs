using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

namespace Ludo.Reactive.Collections
{
    /// <summary>
    /// Represents a set change event.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    [Serializable]
    public struct SetChangeEvent<T>
    {
        /// <summary>
        /// Gets the type of change.
        /// </summary>
        public CollectionChangeType Type { get; }

        /// <summary>
        /// Gets the item involved in the change.
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Gets the timestamp when the change occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the SetChangeEvent struct.
        /// </summary>
        /// <param name="type">The type of change.</param>
        /// <param name="item">The item involved in the change.</param>
        public SetChangeEvent(CollectionChangeType type, T item)
        {
            Type = type;
            Item = item;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a string representation of the set change event.
        /// </summary>
        /// <returns>A string representation of the set change event.</returns>
        public override string ToString()
        {
            return Type switch
            {
                CollectionChangeType.Add => $"Add = {Item}",
                CollectionChangeType.Remove => $"Remove = {Item}",
                CollectionChangeType.Reset => "Reset",
                _ => $"Unknown = {Item}"
            };
        }
    }

    /// <summary>
    /// An observable set that provides notifications when items are added or removed.
    /// Maintains uniqueness of elements and provides efficient set operations.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    [Serializable]
    public class ObservableSet<T> : ISet<T>, INotifyCollectionChanged,
        IObservable<SetChangeEvent<T>>, IDisposable
    {
        [SerializeField] private HashSet<T> _set;
        private Subject<SetChangeEvent<T>> _changeSubject;
        private Subject<SetChangeEvent<T>> _addSubject;
        private Subject<SetChangeEvent<T>> _removeSubject;
        private Subject<int> _countSubject;
        private bool _disposed;

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Gets the number of items in the set.
        /// </summary>
        public int Count => _set.Count;

        /// <summary>
        /// Gets a value indicating whether the set is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets an observable stream of all set changes.
        /// </summary>
        public IObservable<SetChangeEvent<T>> ObserveChanges() => _changeSubject;

        /// <summary>
        /// Gets an observable stream of add events.
        /// </summary>
        public IObservable<SetChangeEvent<T>> ObserveAdd() => _addSubject;

        /// <summary>
        /// Gets an observable stream of remove events.
        /// </summary>
        public IObservable<SetChangeEvent<T>> ObserveRemove() => _removeSubject;

        /// <summary>
        /// Gets an observable stream of count changes.
        /// </summary>
        public IObservable<int> ObserveCountChanged() => _countSubject;

        /// <summary>
        /// Initializes a new instance of the ObservableSet class.
        /// </summary>
        /// <param name="comparer">The equality comparer to use for items.</param>
        public ObservableSet(IEqualityComparer<T> comparer = null)
        {
            _set = new HashSet<T>(comparer ?? EqualityComparer<T>.Default);
            InitializeSubjects();
        }

        /// <summary>
        /// Initializes a new instance of the ObservableSet class with the specified collection.
        /// </summary>
        /// <param name="collection">The collection to copy items from.</param>
        /// <param name="comparer">The equality comparer to use for items.</param>
        public ObservableSet(IEnumerable<T> collection, IEqualityComparer<T> comparer = null)
        {
            _set = new HashSet<T>(collection, comparer ?? EqualityComparer<T>.Default);
            InitializeSubjects();
        }

        private void InitializeSubjects()
        {
            _changeSubject = new Subject<SetChangeEvent<T>>();
            _addSubject = new Subject<SetChangeEvent<T>>();
            _removeSubject = new Subject<SetChangeEvent<T>>();
            _countSubject = new Subject<int>();
        }

        /// <summary>
        /// Adds the specified item to the set.
        /// </summary>
        /// <param name="item">The item to add to the set.</param>
        /// <returns>True if the item was added to the set; false if the item was already present.</returns>
        public bool Add(T item)
        {
            CheckDisposed();
            if (_set.Add(item))
            {
                var addEvent = new SetChangeEvent<T>(CollectionChangeType.Add, item);
                _addSubject.OnNext(addEvent);
                _changeSubject.OnNext(addEvent);
                _countSubject.OnNext(_set.Count);
                
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, item));
                
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds the specified item to the set.
        /// </summary>
        /// <param name="item">The item to add to the set.</param>
        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        /// <summary>
        /// Removes all items from the set.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();
            if (_set.Count == 0)
                return;

            _set.Clear();
            _countSubject.OnNext(0);
            
            var resetEvent = new SetChangeEvent<T>(CollectionChangeType.Reset, default);
            _changeSubject.OnNext(resetEvent);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Determines whether the set contains the specified item.
        /// </summary>
        /// <param name="item">The item to locate in the set.</param>
        /// <returns>True if the set contains the item; otherwise, false.</returns>
        public bool Contains(T item)
        {
            CheckDisposed();
            return _set.Contains(item);
        }

        /// <summary>
        /// Copies the items of the set to an array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The array that is the destination of the items.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            CheckDisposed();
            _set.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the specified item from the set.
        /// </summary>
        /// <param name="item">The item to remove from the set.</param>
        /// <returns>True if the item was successfully removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            CheckDisposed();
            if (_set.Remove(item))
            {
                var removeEvent = new SetChangeEvent<T>(CollectionChangeType.Remove, item);
                _removeSubject.OnNext(removeEvent);
                _changeSubject.OnNext(removeEvent);
                _countSubject.OnNext(_set.Count);
                
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, item));
                
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the set.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the set.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            CheckDisposed();
            return _set.GetEnumerator();
        }

        /// <summary>
        /// Modifies the current set to contain all elements that are present in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        public void UnionWith(IEnumerable<T> other)
        {
            CheckDisposed();
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var addedItems = new List<T>();
            foreach (var item in other)
            {
                if (_set.Add(item))
                {
                    addedItems.Add(item);
                }
            }

            if (addedItems.Count > 0)
            {
                foreach (var item in addedItems)
                {
                    var addEvent = new SetChangeEvent<T>(CollectionChangeType.Add, item);
                    _addSubject.OnNext(addEvent);
                    _changeSubject.OnNext(addEvent);
                }
                
                _countSubject.OnNext(_set.Count);
                
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, addedItems));
            }
        }

        /// <summary>
        /// Modifies the current set to contain only elements that are present in that object and in the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        public void IntersectWith(IEnumerable<T> other)
        {
            CheckDisposed();
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var otherSet = new HashSet<T>(other, _set.Comparer);
            var removedItems = new List<T>();
            
            foreach (var item in _set.ToArray())
            {
                if (!otherSet.Contains(item))
                {
                    _set.Remove(item);
                    removedItems.Add(item);
                }
            }

            if (removedItems.Count > 0)
            {
                foreach (var item in removedItems)
                {
                    var removeEvent = new SetChangeEvent<T>(CollectionChangeType.Remove, item);
                    _removeSubject.OnNext(removeEvent);
                    _changeSubject.OnNext(removeEvent);
                }
                
                _countSubject.OnNext(_set.Count);
                
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, removedItems));
            }
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current set.
        /// </summary>
        /// <param name="other">The collection of items to remove from the set.</param>
        public void ExceptWith(IEnumerable<T> other)
        {
            CheckDisposed();
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var removedItems = new List<T>();
            foreach (var item in other)
            {
                if (_set.Remove(item))
                {
                    removedItems.Add(item);
                }
            }

            if (removedItems.Count > 0)
            {
                foreach (var item in removedItems)
                {
                    var removeEvent = new SetChangeEvent<T>(CollectionChangeType.Remove, item);
                    _removeSubject.OnNext(removeEvent);
                    _changeSubject.OnNext(removeEvent);
                }
                
                _countSubject.OnNext(_set.Count);
                
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, removedItems));
            }
        }

        /// <summary>
        /// Modifies the current set to contain only elements that are present either in that object or in the specified collection, but not both.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            CheckDisposed();
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var otherSet = new HashSet<T>(other, _set.Comparer);
            var addedItems = new List<T>();
            var removedItems = new List<T>();

            // Remove items that are in both sets
            foreach (var item in _set.ToArray())
            {
                if (otherSet.Contains(item))
                {
                    _set.Remove(item);
                    removedItems.Add(item);
                    otherSet.Remove(item);
                }
            }

            // Add items that are only in the other set
            foreach (var item in otherSet)
            {
                if (_set.Add(item))
                {
                    addedItems.Add(item);
                }
            }

            // Emit events for removed items
            foreach (var item in removedItems)
            {
                var removeEvent = new SetChangeEvent<T>(CollectionChangeType.Remove, item);
                _removeSubject.OnNext(removeEvent);
                _changeSubject.OnNext(removeEvent);
            }

            // Emit events for added items
            foreach (var item in addedItems)
            {
                var addEvent = new SetChangeEvent<T>(CollectionChangeType.Add, item);
                _addSubject.OnNext(addEvent);
                _changeSubject.OnNext(addEvent);
            }

            if (addedItems.Count > 0 || removedItems.Count > 0)
            {
                _countSubject.OnNext(_set.Count);
                
                if (removedItems.Count > 0)
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove, removedItems));
                }
                
                if (addedItems.Count > 0)
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, addedItems));
                }
            }
        }

        /// <summary>
        /// Determines whether the current set is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>True if the current set is a subset of other; otherwise, false.</returns>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            CheckDisposed();
            return _set.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>True if the current set is a superset of other; otherwise, false.</returns>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            CheckDisposed();
            return _set.IsSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>True if the current set is a proper subset of other; otherwise, false.</returns>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            CheckDisposed();
            return _set.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>True if the current set is a proper superset of other; otherwise, false.</returns>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            CheckDisposed();
            return _set.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the current set and the specified collection share common elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>True if the current set and other share at least one common element; otherwise, false.</returns>
        public bool Overlaps(IEnumerable<T> other)
        {
            CheckDisposed();
            return _set.Overlaps(other);
        }

        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>True if the current set is equal to other; otherwise, false.</returns>
        public bool SetEquals(IEnumerable<T> other)
        {
            CheckDisposed();
            return _set.SetEquals(other);
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<SetChangeEvent<T>> observer)
        {
            CheckDisposed();
            return _changeSubject.Subscribe(observer);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ObservableSet<T>));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            _changeSubject?.Dispose();
            _addSubject?.Dispose();
            _removeSubject?.Dispose();
            _countSubject?.Dispose();
            
            _set?.Clear();
        }
    }
}
