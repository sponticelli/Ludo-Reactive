using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludo.Reactive.Collections
{
    /// <summary>
    /// Represents the type of change that occurred in a collection.
    /// </summary>
    public enum CollectionChangeType
    {
        /// <summary>
        /// An item was added to the collection.
        /// </summary>
        Add,

        /// <summary>
        /// An item was removed from the collection.
        /// </summary>
        Remove,

        /// <summary>
        /// An item was replaced in the collection.
        /// </summary>
        Replace,

        /// <summary>
        /// An item was moved within the collection.
        /// </summary>
        Move,

        /// <summary>
        /// The collection was reset (cleared).
        /// </summary>
        Reset
    }

    /// <summary>
    /// Represents a single change in a collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    [Serializable]
    public struct CollectionChange<T>
    {
        /// <summary>
        /// Gets the type of change.
        /// </summary>
        public CollectionChangeType Type { get; }

        /// <summary>
        /// Gets the index where the change occurred.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the old value (for Remove and Replace operations).
        /// </summary>
        public T OldValue { get; }

        /// <summary>
        /// Gets the new value (for Add and Replace operations).
        /// </summary>
        public T NewValue { get; }

        /// <summary>
        /// Gets the new index (for Move operations).
        /// </summary>
        public int NewIndex { get; }

        /// <summary>
        /// Gets the timestamp when the change occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the CollectionChange struct for Add operations.
        /// </summary>
        /// <param name="type">The type of change.</param>
        /// <param name="index">The index where the change occurred.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public CollectionChange(CollectionChangeType type, int index, T oldValue, T newValue)
        {
            Type = type;
            Index = index;
            OldValue = oldValue;
            NewValue = newValue;
            NewIndex = -1;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the CollectionChange struct for Move operations.
        /// </summary>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        /// <param name="value">The value that was moved.</param>
        public CollectionChange(int oldIndex, int newIndex, T value)
        {
            Type = CollectionChangeType.Move;
            Index = oldIndex;
            NewIndex = newIndex;
            OldValue = value;
            NewValue = value;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a CollectionChange for an Add operation.
        /// </summary>
        /// <param name="index">The index where the item was added.</param>
        /// <param name="value">The value that was added.</param>
        /// <returns>A CollectionChange representing the add operation.</returns>
        public static CollectionChange<T> Add(int index, T value)
        {
            return new CollectionChange<T>(CollectionChangeType.Add, index, default, value);
        }

        /// <summary>
        /// Creates a CollectionChange for a Remove operation.
        /// </summary>
        /// <param name="index">The index where the item was removed.</param>
        /// <param name="value">The value that was removed.</param>
        /// <returns>A CollectionChange representing the remove operation.</returns>
        public static CollectionChange<T> Remove(int index, T value)
        {
            return new CollectionChange<T>(CollectionChangeType.Remove, index, value, default);
        }

        /// <summary>
        /// Creates a CollectionChange for a Replace operation.
        /// </summary>
        /// <param name="index">The index where the item was replaced.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>A CollectionChange representing the replace operation.</returns>
        public static CollectionChange<T> Replace(int index, T oldValue, T newValue)
        {
            return new CollectionChange<T>(CollectionChangeType.Replace, index, oldValue, newValue);
        }

        /// <summary>
        /// Creates a CollectionChange for a Move operation.
        /// </summary>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        /// <param name="value">The value that was moved.</param>
        /// <returns>A CollectionChange representing the move operation.</returns>
        public static CollectionChange<T> Move(int oldIndex, int newIndex, T value)
        {
            return new CollectionChange<T>(oldIndex, newIndex, value);
        }

        /// <summary>
        /// Returns a string representation of the collection change.
        /// </summary>
        /// <returns>A string representation of the collection change.</returns>
        public override string ToString()
        {
            return Type switch
            {
                CollectionChangeType.Add => $"Add[{Index}] = {NewValue}",
                CollectionChangeType.Remove => $"Remove[{Index}] = {OldValue}",
                CollectionChangeType.Replace => $"Replace[{Index}] {OldValue} -> {NewValue}",
                CollectionChangeType.Move => $"Move[{Index} -> {NewIndex}] = {OldValue}",
                CollectionChangeType.Reset => "Reset",
                _ => $"Unknown[{Index}]"
            };
        }
    }

    /// <summary>
    /// Represents a set of changes that occurred in a collection.
    /// Provides efficient batch processing of multiple changes.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    [Serializable]
    public class CollectionChangeSet<T>
    {
        private readonly List<CollectionChange<T>> _changes;

        /// <summary>
        /// Gets the changes in this change set.
        /// </summary>
        public IReadOnlyList<CollectionChange<T>> Changes => _changes.AsReadOnly();

        /// <summary>
        /// Gets the number of changes in this set.
        /// </summary>
        public int Count => _changes.Count;

        /// <summary>
        /// Gets whether this change set is empty.
        /// </summary>
        public bool IsEmpty => _changes.Count == 0;

        /// <summary>
        /// Gets the timestamp when this change set was created.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the CollectionChangeSet class.
        /// </summary>
        public CollectionChangeSet()
        {
            _changes = new List<CollectionChange<T>>();
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the CollectionChangeSet class with the specified changes.
        /// </summary>
        /// <param name="changes">The changes to include in this set.</param>
        public CollectionChangeSet(IEnumerable<CollectionChange<T>> changes)
        {
            _changes = new List<CollectionChange<T>>(changes ?? throw new ArgumentNullException(nameof(changes)));
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a change to this change set.
        /// </summary>
        /// <param name="change">The change to add.</param>
        public void Add(CollectionChange<T> change)
        {
            _changes.Add(change);
        }

        /// <summary>
        /// Adds multiple changes to this change set.
        /// </summary>
        /// <param name="changes">The changes to add.</param>
        public void AddRange(IEnumerable<CollectionChange<T>> changes)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));

            _changes.AddRange(changes);
        }

        /// <summary>
        /// Gets all changes of the specified type.
        /// </summary>
        /// <param name="type">The type of changes to get.</param>
        /// <returns>An enumerable of changes of the specified type.</returns>
        public IEnumerable<CollectionChange<T>> GetChanges(CollectionChangeType type)
        {
            return _changes.Where(c => c.Type == type);
        }

        /// <summary>
        /// Gets all added items in this change set.
        /// </summary>
        /// <returns>An enumerable of added items with their indices.</returns>
        public IEnumerable<(int Index, T Item)> GetAddedItems()
        {
            return GetChanges(CollectionChangeType.Add)
                .Select(c => (c.Index, c.NewValue));
        }

        /// <summary>
        /// Gets all removed items in this change set.
        /// </summary>
        /// <returns>An enumerable of removed items with their indices.</returns>
        public IEnumerable<(int Index, T Item)> GetRemovedItems()
        {
            return GetChanges(CollectionChangeType.Remove)
                .Select(c => (c.Index, c.OldValue));
        }

        /// <summary>
        /// Gets all replaced items in this change set.
        /// </summary>
        /// <returns>An enumerable of replaced items with their indices and old/new values.</returns>
        public IEnumerable<(int Index, T OldValue, T NewValue)> GetReplacedItems()
        {
            return GetChanges(CollectionChangeType.Replace)
                .Select(c => (c.Index, c.OldValue, c.NewValue));
        }

        /// <summary>
        /// Gets all moved items in this change set.
        /// </summary>
        /// <returns>An enumerable of moved items with their old and new indices.</returns>
        public IEnumerable<(int OldIndex, int NewIndex, T Item)> GetMovedItems()
        {
            return GetChanges(CollectionChangeType.Move)
                .Select(c => (c.Index, c.NewIndex, c.OldValue));
        }

        /// <summary>
        /// Determines whether this change set contains any changes of the specified type.
        /// </summary>
        /// <param name="type">The type of change to check for.</param>
        /// <returns>True if the change set contains changes of the specified type; otherwise, false.</returns>
        public bool HasChanges(CollectionChangeType type)
        {
            return _changes.Any(c => c.Type == type);
        }

        /// <summary>
        /// Determines whether this change set contains any structural changes (add, remove, move).
        /// </summary>
        /// <returns>True if the change set contains structural changes; otherwise, false.</returns>
        public bool HasStructuralChanges()
        {
            return _changes.Any(c => c.Type == CollectionChangeType.Add ||
                                   c.Type == CollectionChangeType.Remove ||
                                   c.Type == CollectionChangeType.Move ||
                                   c.Type == CollectionChangeType.Reset);
        }

        /// <summary>
        /// Creates a summary of the changes in this change set.
        /// </summary>
        /// <returns>A string summary of the changes.</returns>
        public string GetSummary()
        {
            if (IsEmpty)
                return "No changes";

            var summary = new List<string>();
            
            var addCount = GetChanges(CollectionChangeType.Add).Count();
            if (addCount > 0)
                summary.Add($"{addCount} added");

            var removeCount = GetChanges(CollectionChangeType.Remove).Count();
            if (removeCount > 0)
                summary.Add($"{removeCount} removed");

            var replaceCount = GetChanges(CollectionChangeType.Replace).Count();
            if (replaceCount > 0)
                summary.Add($"{replaceCount} replaced");

            var moveCount = GetChanges(CollectionChangeType.Move).Count();
            if (moveCount > 0)
                summary.Add($"{moveCount} moved");

            var resetCount = GetChanges(CollectionChangeType.Reset).Count();
            if (resetCount > 0)
                summary.Add($"{resetCount} reset");

            return string.Join(", ", summary);
        }

        /// <summary>
        /// Returns a string representation of this change set.
        /// </summary>
        /// <returns>A string representation of this change set.</returns>
        public override string ToString()
        {
            return $"ChangeSet: {GetSummary()} @ {Timestamp:HH:mm:ss.fff}";
        }
    }
}
