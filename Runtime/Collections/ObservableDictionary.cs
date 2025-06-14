using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

namespace Ludo.Reactive.Collections
{
    /// <summary>
    /// Represents a dictionary change event.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    [Serializable]
    public struct DictionaryChangeEvent<TKey, TValue>
    {
        /// <summary>
        /// Gets the type of change.
        /// </summary>
        public CollectionChangeType Type { get; }

        /// <summary>
        /// Gets the key involved in the change.
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        /// Gets the old value (for Remove and Replace operations).
        /// </summary>
        public TValue OldValue { get; }

        /// <summary>
        /// Gets the new value (for Add and Replace operations).
        /// </summary>
        public TValue NewValue { get; }

        /// <summary>
        /// Gets the timestamp when the change occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the DictionaryChangeEvent struct.
        /// </summary>
        /// <param name="type">The type of change.</param>
        /// <param name="key">The key involved in the change.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public DictionaryChangeEvent(CollectionChangeType type, TKey key, TValue oldValue, TValue newValue)
        {
            Type = type;
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a string representation of the dictionary change event.
        /// </summary>
        /// <returns>A string representation of the dictionary change event.</returns>
        public override string ToString()
        {
            return Type switch
            {
                CollectionChangeType.Add => $"Add[{Key}] = {NewValue}",
                CollectionChangeType.Remove => $"Remove[{Key}] = {OldValue}",
                CollectionChangeType.Replace => $"Replace[{Key}] {OldValue} -> {NewValue}",
                CollectionChangeType.Reset => "Reset",
                _ => $"Unknown[{Key}]"
            };
        }
    }

    /// <summary>
    /// An observable dictionary that provides notifications when key-value pairs are added, removed, or modified.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    [Serializable]
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged,
        IObservable<DictionaryChangeEvent<TKey, TValue>>, IDisposable
    {
        [SerializeField] private Dictionary<TKey, TValue> _dictionary;
        private Subject<DictionaryChangeEvent<TKey, TValue>> _changeSubject;
        private Subject<DictionaryChangeEvent<TKey, TValue>> _addSubject;
        private Subject<DictionaryChangeEvent<TKey, TValue>> _removeSubject;
        private Subject<DictionaryChangeEvent<TKey, TValue>> _replaceSubject;
        private Subject<int> _countSubject;
        private readonly IEqualityComparer<TValue> _valueComparer;
        private bool _disposed;

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Gets the number of key-value pairs in the dictionary.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public ICollection<TKey> Keys => _dictionary.Keys;

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public ICollection<TValue> Values => _dictionary.Values;

        /// <summary>
        /// Gets an observable stream of all dictionary changes.
        /// </summary>
        public IObservable<DictionaryChangeEvent<TKey, TValue>> ObserveChanges() => _changeSubject;

        /// <summary>
        /// Gets an observable stream of add events.
        /// </summary>
        public IObservable<DictionaryChangeEvent<TKey, TValue>> ObserveAdd() => _addSubject;

        /// <summary>
        /// Gets an observable stream of remove events.
        /// </summary>
        public IObservable<DictionaryChangeEvent<TKey, TValue>> ObserveRemove() => _removeSubject;

        /// <summary>
        /// Gets an observable stream of replace events.
        /// </summary>
        public IObservable<DictionaryChangeEvent<TKey, TValue>> ObserveReplace() => _replaceSubject;

        /// <summary>
        /// Gets an observable stream of count changes.
        /// </summary>
        public IObservable<int> ObserveCountChanged() => _countSubject;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value associated with the specified key.</returns>
        public TValue this[TKey key]
        {
            get
            {
                CheckDisposed();
                return _dictionary[key];
            }
            set
            {
                CheckDisposed();
                var hasKey = _dictionary.TryGetValue(key, out var oldValue);
                
                if (hasKey)
                {
                    if (!_valueComparer.Equals(oldValue, value))
                    {
                        _dictionary[key] = value;
                        
                        var replaceEvent = new DictionaryChangeEvent<TKey, TValue>(
                            CollectionChangeType.Replace, key, oldValue, value);
                        _replaceSubject.OnNext(replaceEvent);
                        _changeSubject.OnNext(replaceEvent);
                        
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace, 
                            new KeyValuePair<TKey, TValue>(key, value),
                            new KeyValuePair<TKey, TValue>(key, oldValue)));
                    }
                }
                else
                {
                    _dictionary[key] = value;
                    
                    var addEvent = new DictionaryChangeEvent<TKey, TValue>(
                        CollectionChangeType.Add, key, default, value);
                    _addSubject.OnNext(addEvent);
                    _changeSubject.OnNext(addEvent);
                    _countSubject.OnNext(_dictionary.Count);
                    
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the ObservableDictionary class.
        /// </summary>
        /// <param name="comparer">The equality comparer to use for keys.</param>
        /// <param name="valueComparer">The equality comparer to use for values.</param>
        public ObservableDictionary(IEqualityComparer<TKey> comparer = null, IEqualityComparer<TValue> valueComparer = null)
        {
            _dictionary = new Dictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default);
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            InitializeSubjects();
        }

        /// <summary>
        /// Initializes a new instance of the ObservableDictionary class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the dictionary.</param>
        /// <param name="comparer">The equality comparer to use for keys.</param>
        /// <param name="valueComparer">The equality comparer to use for values.</param>
        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer = null, IEqualityComparer<TValue> valueComparer = null)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity, comparer ?? EqualityComparer<TKey>.Default);
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            InitializeSubjects();
        }

        /// <summary>
        /// Initializes a new instance of the ObservableDictionary class with the specified dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to copy elements from.</param>
        /// <param name="comparer">The equality comparer to use for keys.</param>
        /// <param name="valueComparer">The equality comparer to use for values.</param>
        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer = null, IEqualityComparer<TValue> valueComparer = null)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary, comparer ?? EqualityComparer<TKey>.Default);
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            InitializeSubjects();
        }

        private void InitializeSubjects()
        {
            _changeSubject = new Subject<DictionaryChangeEvent<TKey, TValue>>();
            _addSubject = new Subject<DictionaryChangeEvent<TKey, TValue>>();
            _removeSubject = new Subject<DictionaryChangeEvent<TKey, TValue>>();
            _replaceSubject = new Subject<DictionaryChangeEvent<TKey, TValue>>();
            _countSubject = new Subject<int>();
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        public void Add(TKey key, TValue value)
        {
            CheckDisposed();
            _dictionary.Add(key, value);
            
            var addEvent = new DictionaryChangeEvent<TKey, TValue>(
                CollectionChangeType.Add, key, default, value);
            _addSubject.OnNext(addEvent);
            _changeSubject.OnNext(addEvent);
            _countSubject.OnNext(_dictionary.Count);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
        }

        /// <summary>
        /// Adds the specified key-value pair to the dictionary.
        /// </summary>
        /// <param name="item">The key-value pair to add.</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();
            if (_dictionary.Count == 0)
                return;

            _dictionary.Clear();
            _countSubject.OnNext(0);
            
            var resetEvent = new DictionaryChangeEvent<TKey, TValue>(
                CollectionChangeType.Reset, default, default, default);
            _changeSubject.OnNext(resetEvent);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key-value pair.
        /// </summary>
        /// <param name="item">The key-value pair to locate in the dictionary.</param>
        /// <returns>True if the dictionary contains the key-value pair; otherwise, false.</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            CheckDisposed();
            return _dictionary.TryGetValue(item.Key, out var value) && _valueComparer.Equals(value, item.Value);
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary.</param>
        /// <returns>True if the dictionary contains an element with the specified key; otherwise, false.</returns>
        public bool ContainsKey(TKey key)
        {
            CheckDisposed();
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Copies the key-value pairs of the dictionary to an array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The array that is the destination of the key-value pairs.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            CheckDisposed();
            ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the dictionary.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            CheckDisposed();
            return _dictionary.GetEnumerator();
        }

        /// <summary>
        /// Removes the value with the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>True if the element is successfully found and removed; otherwise, false.</returns>
        public bool Remove(TKey key)
        {
            CheckDisposed();
            if (_dictionary.TryGetValue(key, out var value))
            {
                _dictionary.Remove(key);
                
                var removeEvent = new DictionaryChangeEvent<TKey, TValue>(
                    CollectionChangeType.Remove, key, value, default);
                _removeSubject.OnNext(removeEvent);
                _changeSubject.OnNext(removeEvent);
                _countSubject.OnNext(_dictionary.Count);
                
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value)));
                
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the specified key-value pair from the dictionary.
        /// </summary>
        /// <param name="item">The key-value pair to remove.</param>
        /// <returns>True if the key-value pair was successfully removed; otherwise, false.</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            CheckDisposed();
            if (Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found.</param>
        /// <returns>True if the dictionary contains an element with the specified key; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            CheckDisposed();
            return _dictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<DictionaryChangeEvent<TKey, TValue>> observer)
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
                throw new ObjectDisposedException(nameof(ObservableDictionary<TKey, TValue>));
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
            _replaceSubject?.Dispose();
            _countSubject?.Dispose();
            
            _dictionary?.Clear();
        }
    }
}
