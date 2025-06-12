using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents an event that occurs when an item is added to a reactive collection.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    [Serializable]
    public struct CollectionAddEvent<T>
    {
        /// <summary>
        /// Gets the index where the item was added.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the item that was added.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initializes a new instance of the CollectionAddEvent struct.
        /// </summary>
        /// <param name="index">The index where the item was added.</param>
        /// <param name="value">The item that was added.</param>
        public CollectionAddEvent(int index, T value)
        {
            Index = index;
            Value = value;
        }

        /// <summary>
        /// Returns a string representation of the add event.
        /// </summary>
        /// <returns>A string representation of the add event.</returns>
        public override string ToString()
        {
            return $"Add[{Index}] = {Value}";
        }
    }

    /// <summary>
    /// Represents an event that occurs when an item is removed from a reactive collection.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    [Serializable]
    public struct CollectionRemoveEvent<T>
    {
        /// <summary>
        /// Gets the index where the item was removed.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the item that was removed.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initializes a new instance of the CollectionRemoveEvent struct.
        /// </summary>
        /// <param name="index">The index where the item was removed.</param>
        /// <param name="value">The item that was removed.</param>
        public CollectionRemoveEvent(int index, T value)
        {
            Index = index;
            Value = value;
        }

        /// <summary>
        /// Returns a string representation of the remove event.
        /// </summary>
        /// <returns>A string representation of the remove event.</returns>
        public override string ToString()
        {
            return $"Remove[{Index}] = {Value}";
        }
    }

    /// <summary>
    /// Represents an event that occurs when an item is replaced in a reactive collection.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    [Serializable]
    public struct CollectionReplaceEvent<T>
    {
        /// <summary>
        /// Gets the index where the item was replaced.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the old value that was replaced.
        /// </summary>
        public T OldValue { get; }

        /// <summary>
        /// Gets the new value that replaced the old value.
        /// </summary>
        public T NewValue { get; }

        /// <summary>
        /// Initializes a new instance of the CollectionReplaceEvent struct.
        /// </summary>
        /// <param name="index">The index where the item was replaced.</param>
        /// <param name="oldValue">The old value that was replaced.</param>
        /// <param name="newValue">The new value that replaced the old value.</param>
        public CollectionReplaceEvent(int index, T oldValue, T newValue)
        {
            Index = index;
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Returns a string representation of the replace event.
        /// </summary>
        /// <returns>A string representation of the replace event.</returns>
        public override string ToString()
        {
            return $"Replace[{Index}] {OldValue} -> {NewValue}";
        }
    }

    /// <summary>
    /// Represents an event that occurs when an item is moved in a reactive collection.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    [Serializable]
    public struct CollectionMoveEvent<T>
    {
        /// <summary>
        /// Gets the old index of the item.
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        /// Gets the new index of the item.
        /// </summary>
        public int NewIndex { get; }

        /// <summary>
        /// Gets the item that was moved.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initializes a new instance of the CollectionMoveEvent struct.
        /// </summary>
        /// <param name="oldIndex">The old index of the item.</param>
        /// <param name="newIndex">The new index of the item.</param>
        /// <param name="value">The item that was moved.</param>
        public CollectionMoveEvent(int oldIndex, int newIndex, T value)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            Value = value;
        }

        /// <summary>
        /// Returns a string representation of the move event.
        /// </summary>
        /// <returns>A string representation of the move event.</returns>
        public override string ToString()
        {
            return $"Move[{OldIndex} -> {NewIndex}] = {Value}";
        }
    }

    /// <summary>
    /// Represents an event that occurs when a reactive collection is reset (cleared).
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    [Serializable]
    public struct CollectionResetEvent<T>
    {
        /// <summary>
        /// Gets the items that were in the collection before it was reset.
        /// </summary>
        public IReadOnlyList<T> OldItems { get; }

        /// <summary>
        /// Initializes a new instance of the CollectionResetEvent struct.
        /// </summary>
        /// <param name="oldItems">The items that were in the collection before it was reset.</param>
        public CollectionResetEvent(IReadOnlyList<T> oldItems)
        {
            OldItems = oldItems;
        }

        /// <summary>
        /// Returns a string representation of the reset event.
        /// </summary>
        /// <returns>A string representation of the reset event.</returns>
        public override string ToString()
        {
            return $"Reset (removed {OldItems?.Count ?? 0} items)";
        }
    }
}
