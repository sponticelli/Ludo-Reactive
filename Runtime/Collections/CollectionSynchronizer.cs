using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ludo.Reactive.Collections
{
    /// <summary>
    /// Defines strategies for resolving conflicts when synchronizing collections.
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        /// <summary>
        /// The source collection takes precedence.
        /// </summary>
        SourceWins,

        /// <summary>
        /// The target collection takes precedence.
        /// </summary>
        TargetWins,

        /// <summary>
        /// The most recent change takes precedence.
        /// </summary>
        MostRecentWins,

        /// <summary>
        /// Use a custom conflict resolution function.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Synchronizes changes between multiple observable collections.
    /// Provides conflict resolution and bidirectional synchronization.
    /// </summary>
    /// <typeparam name="T">The type of items in the collections.</typeparam>
    public class CollectionSynchronizer<T> : IDisposable
    {
        private readonly List<SynchronizedCollection<T>> _collections;
        private readonly ConflictResolutionStrategy _conflictStrategy;
        private readonly Func<T, T, T> _customResolver;
        private readonly IEqualityComparer<T> _comparer;
        private readonly object _lock = new object();
        private bool _disposed;
        private bool _synchronizing;

        /// <summary>
        /// Gets the number of synchronized collections.
        /// </summary>
        public int CollectionCount => _collections.Count;

        /// <summary>
        /// Gets the conflict resolution strategy being used.
        /// </summary>
        public ConflictResolutionStrategy ConflictStrategy => _conflictStrategy;

        /// <summary>
        /// Initializes a new instance of the CollectionSynchronizer class.
        /// </summary>
        /// <param name="conflictStrategy">The strategy to use for resolving conflicts.</param>
        /// <param name="customResolver">Custom resolver function for conflicts (used when strategy is Custom).</param>
        /// <param name="comparer">The equality comparer to use for items.</param>
        public CollectionSynchronizer(
            ConflictResolutionStrategy conflictStrategy = ConflictResolutionStrategy.SourceWins,
            Func<T, T, T> customResolver = null,
            IEqualityComparer<T> comparer = null)
        {
            _collections = new List<SynchronizedCollection<T>>();
            _conflictStrategy = conflictStrategy;
            _customResolver = customResolver;
            _comparer = comparer ?? EqualityComparer<T>.Default;

            if (_conflictStrategy == ConflictResolutionStrategy.Custom && _customResolver == null)
            {
                throw new ArgumentException("Custom resolver function is required when using Custom conflict strategy");
            }
        }

        /// <summary>
        /// Adds a collection to the synchronization group.
        /// </summary>
        /// <param name="collection">The collection to add.</param>
        /// <param name="name">Optional name for the collection.</param>
        /// <returns>A disposable that removes the collection from synchronization when disposed.</returns>
        public IDisposable AddCollection(ObservableList<T> collection, string name = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            CheckDisposed();

            lock (_lock)
            {
                var syncCollection = new SynchronizedCollection<T>(collection, name ?? $"Collection_{_collections.Count}");

                // If this is not the first collection, synchronize existing items
                if (_collections.Count > 0)
                {
                    // Find the collection with the most items to use as source
                    var sourceCollection = _collections.OrderByDescending(c => c.Collection.Count).First();

                    // Synchronize the new collection to match the source
                    _synchronizing = true;
                    try
                    {
                        collection.Clear();
                        foreach (var item in sourceCollection.Collection)
                        {
                            collection.Add(item);
                        }
                    }
                    finally
                    {
                        _synchronizing = false;
                    }
                }

                _collections.Add(syncCollection);

                // Subscribe to changes
                var subscription = collection.Subscribe(changeSet =>
                {
                    if (!_synchronizing)
                    {
                        SynchronizeChanges(syncCollection, changeSet);
                    }
                });

                syncCollection.Subscription = subscription;

                return Disposable.Create(() =>
                {
                    lock (_lock)
                    {
                        subscription?.Dispose();
                        _collections.Remove(syncCollection);
                    }
                });
            }
        }

        /// <summary>
        /// Synchronizes all collections to match the specified source collection.
        /// </summary>
        /// <param name="sourceCollection">The collection to use as the source.</param>
        public void SynchronizeToSource(ObservableList<T> sourceCollection)
        {
            if (sourceCollection == null)
                throw new ArgumentNullException(nameof(sourceCollection));

            CheckDisposed();

            lock (_lock)
            {
                _synchronizing = true;
                try
                {
                    var sourceItems = sourceCollection.ToList();
                    
                    foreach (var syncCollection in _collections)
                    {
                        if (ReferenceEquals(syncCollection.Collection, sourceCollection))
                            continue;

                        SynchronizeCollectionToSource(syncCollection.Collection, sourceItems);
                    }
                }
                finally
                {
                    _synchronizing = false;
                }
            }
        }

        /// <summary>
        /// Forces a full synchronization of all collections.
        /// Uses the first collection as the source.
        /// </summary>
        public void ForceSynchronization()
        {
            CheckDisposed();

            lock (_lock)
            {
                if (_collections.Count == 0)
                    return;

                var sourceCollection = _collections[0].Collection;
                SynchronizeToSource(sourceCollection);
            }
        }

        /// <summary>
        /// Gets statistics about the synchronization state.
        /// </summary>
        /// <returns>Synchronization statistics.</returns>
        public SynchronizationStats GetStats()
        {
            CheckDisposed();

            lock (_lock)
            {
                var totalItems = _collections.Sum(c => c.Collection.Count);
                var averageItems = _collections.Count > 0 ? totalItems / (double)_collections.Count : 0;

                var itemCounts = _collections.Select(c => c.Collection.Count).ToArray();
                var minItems = itemCounts.Length > 0 ? itemCounts.Min() : 0;
                var maxItems = itemCounts.Length > 0 ? itemCounts.Max() : 0;
                var isInSync = itemCounts.Length <= 1 || itemCounts.All(count => count == itemCounts[0]);

                return new SynchronizationStats(
                    collectionCount: _collections.Count,
                    totalItems: totalItems,
                    averageItems: averageItems,
                    minItems: minItems,
                    maxItems: maxItems,
                    isInSync: isInSync
                );
            }
        }

        private void SynchronizeChanges(SynchronizedCollection<T> sourceCollection, CollectionChangeSet<T> changeSet)
        {
            lock (_lock)
            {
                _synchronizing = true;
                try
                {
                    foreach (var targetCollection in _collections)
                    {
                        if (ReferenceEquals(targetCollection, sourceCollection))
                            continue;

                        ApplyChangesToCollection(targetCollection.Collection, changeSet);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Error synchronizing collections: {ex}");
                }
                finally
                {
                    _synchronizing = false;
                }
            }
        }

        private void ApplyChangesToCollection(ObservableList<T> targetCollection, CollectionChangeSet<T> changeSet)
        {
            foreach (var change in changeSet.Changes)
            {
                try
                {
                    switch (change.Type)
                    {
                        case CollectionChangeType.Add:
                            if (change.Index >= 0 && change.Index <= targetCollection.Count)
                            {
                                targetCollection.Insert(change.Index, change.NewValue);
                            }
                            else
                            {
                                targetCollection.Add(change.NewValue);
                            }
                            break;

                        case CollectionChangeType.Remove:
                            if (change.Index >= 0 && change.Index < targetCollection.Count)
                            {
                                var currentItem = targetCollection[change.Index];
                                if (_comparer.Equals(currentItem, change.OldValue))
                                {
                                    targetCollection.RemoveAt(change.Index);
                                }
                                else
                                {
                                    // Item at index doesn't match, try to find and remove by value
                                    targetCollection.Remove(change.OldValue);
                                }
                            }
                            break;

                        case CollectionChangeType.Replace:
                            if (change.Index >= 0 && change.Index < targetCollection.Count)
                            {
                                var currentItem = targetCollection[change.Index];
                                var resolvedValue = ResolveConflict(currentItem, change.NewValue);
                                targetCollection[change.Index] = resolvedValue;
                            }
                            break;

                        case CollectionChangeType.Reset:
                            targetCollection.Clear();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Error applying change {change}: {ex}");
                }
            }
        }

        private void SynchronizeCollectionToSource(ObservableList<T> targetCollection, List<T> sourceItems)
        {
            // Clear target and add all source items
            targetCollection.Clear();
            targetCollection.AddRange(sourceItems);
        }

        private T ResolveConflict(T currentValue, T newValue)
        {
            return _conflictStrategy switch
            {
                ConflictResolutionStrategy.SourceWins => newValue,
                ConflictResolutionStrategy.TargetWins => currentValue,
                ConflictResolutionStrategy.MostRecentWins => newValue, // Assume new value is more recent
                ConflictResolutionStrategy.Custom => _customResolver(currentValue, newValue),
                _ => newValue
            };
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CollectionSynchronizer<T>));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;

                foreach (var collection in _collections)
                {
                    collection.Subscription?.Dispose();
                }
                _collections.Clear();
            }
        }
    }

    /// <summary>
    /// Represents a collection that is part of a synchronization group.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    internal class SynchronizedCollection<T>
    {
        public ObservableList<T> Collection { get; }
        public string Name { get; }
        public IDisposable Subscription { get; set; }

        public SynchronizedCollection(ObservableList<T> collection, string name)
        {
            Collection = collection ?? throw new ArgumentNullException(nameof(collection));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }

    /// <summary>
    /// Provides statistics about collection synchronization.
    /// </summary>
    public struct SynchronizationStats
    {
        /// <summary>
        /// Gets the number of collections being synchronized.
        /// </summary>
        public int CollectionCount { get; }

        /// <summary>
        /// Gets the total number of items across all collections.
        /// </summary>
        public int TotalItems { get; }

        /// <summary>
        /// Gets the average number of items per collection.
        /// </summary>
        public double AverageItems { get; }

        /// <summary>
        /// Gets the minimum number of items in any collection.
        /// </summary>
        public int MinItems { get; }

        /// <summary>
        /// Gets the maximum number of items in any collection.
        /// </summary>
        public int MaxItems { get; }

        /// <summary>
        /// Gets whether all collections are in sync (have the same items).
        /// </summary>
        public bool IsInSync { get; }

        /// <summary>
        /// Initializes a new instance of the SynchronizationStats struct.
        /// </summary>
        /// <param name="collectionCount">The number of collections being synchronized.</param>
        /// <param name="totalItems">The total number of items across all collections.</param>
        /// <param name="averageItems">The average number of items per collection.</param>
        /// <param name="minItems">The minimum number of items in any collection.</param>
        /// <param name="maxItems">The maximum number of items in any collection.</param>
        /// <param name="isInSync">Whether all collections are in sync.</param>
        public SynchronizationStats(int collectionCount, int totalItems, double averageItems,
            int minItems, int maxItems, bool isInSync)
        {
            CollectionCount = collectionCount;
            TotalItems = totalItems;
            AverageItems = averageItems;
            MinItems = minItems;
            MaxItems = maxItems;
            IsInSync = isInSync;
        }

        /// <summary>
        /// Returns a string representation of the synchronization statistics.
        /// </summary>
        /// <returns>A string representation of the statistics.</returns>
        public override string ToString()
        {
            return $"Collections: {CollectionCount}, Total Items: {TotalItems}, " +
                   $"Average: {AverageItems:F1}, Range: {MinItems}-{MaxItems}, " +
                   $"In Sync: {IsInSync}";
        }
    }
}
