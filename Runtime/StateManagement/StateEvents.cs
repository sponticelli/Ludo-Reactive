using System;
using System.Collections.Generic;

namespace Ludo.Reactive.StateManagement
{
    /// <summary>
    /// Represents an event that occurs when the state changes in a ReactiveStore.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    [Serializable]
    public struct StateChangedEvent<TState>
    {
        /// <summary>
        /// Gets the previous state before the change.
        /// </summary>
        public TState PreviousState { get; }

        /// <summary>
        /// Gets the current state after the change.
        /// </summary>
        public TState CurrentState { get; }

        /// <summary>
        /// Gets the action that caused the state change.
        /// </summary>
        public IAction Action { get; }

        /// <summary>
        /// Gets the timestamp when the state change occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the StateChangedEvent struct.
        /// </summary>
        /// <param name="previousState">The previous state.</param>
        /// <param name="currentState">The current state.</param>
        /// <param name="action">The action that caused the change.</param>
        public StateChangedEvent(TState previousState, TState currentState, IAction action)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Action = action;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a string representation of the state change event.
        /// </summary>
        /// <returns>A string representation of the state change event.</returns>
        public override string ToString()
        {
            return $"StateChanged: {Action} @ {Timestamp:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// Represents an event that occurs when an action is dispatched to a ReactiveStore.
    /// </summary>
    [Serializable]
    public struct ActionDispatchedEvent
    {
        /// <summary>
        /// Gets the action that was dispatched.
        /// </summary>
        public IAction Action { get; }

        /// <summary>
        /// Gets the timestamp when the action was dispatched.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets whether the action was successfully processed.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the error that occurred during action processing, if any.
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// Initializes a new instance of the ActionDispatchedEvent struct.
        /// </summary>
        /// <param name="action">The action that was dispatched.</param>
        /// <param name="success">Whether the action was successfully processed.</param>
        /// <param name="error">The error that occurred, if any.</param>
        public ActionDispatchedEvent(IAction action, bool success, Exception error = null)
        {
            Action = action;
            Success = success;
            Error = error;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a string representation of the action dispatched event.
        /// </summary>
        /// <returns>A string representation of the action dispatched event.</returns>
        public override string ToString()
        {
            var status = Success ? "Success" : $"Failed: {Error?.Message}";
            return $"ActionDispatched: {Action} - {status} @ {Timestamp:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// Represents an event that occurs when a state selector's result changes.
    /// </summary>
    /// <typeparam name="TResult">The type of the selector result.</typeparam>
    [Serializable]
    public struct SelectorChangedEvent<TResult>
    {
        /// <summary>
        /// Gets the previous result of the selector.
        /// </summary>
        public TResult PreviousResult { get; }

        /// <summary>
        /// Gets the current result of the selector.
        /// </summary>
        public TResult CurrentResult { get; }

        /// <summary>
        /// Gets the selector that produced this result.
        /// </summary>
        public string SelectorName { get; }

        /// <summary>
        /// Gets the timestamp when the selector result changed.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the SelectorChangedEvent struct.
        /// </summary>
        /// <param name="previousResult">The previous result.</param>
        /// <param name="currentResult">The current result.</param>
        /// <param name="selectorName">The name of the selector.</param>
        public SelectorChangedEvent(TResult previousResult, TResult currentResult, string selectorName)
        {
            PreviousResult = previousResult;
            CurrentResult = currentResult;
            SelectorName = selectorName;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a string representation of the selector changed event.
        /// </summary>
        /// <returns>A string representation of the selector changed event.</returns>
        public override string ToString()
        {
            return $"SelectorChanged: {SelectorName} @ {Timestamp:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// Represents an event that occurs when the store's state is persisted.
    /// </summary>
    [Serializable]
    public struct StatePersistenceEvent
    {
        /// <summary>
        /// Gets the type of persistence operation.
        /// </summary>
        public PersistenceOperation Operation { get; }

        /// <summary>
        /// Gets whether the persistence operation was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the error that occurred during persistence, if any.
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// Gets the timestamp when the persistence operation occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the StatePersistenceEvent struct.
        /// </summary>
        /// <param name="operation">The type of persistence operation.</param>
        /// <param name="success">Whether the operation was successful.</param>
        /// <param name="error">The error that occurred, if any.</param>
        public StatePersistenceEvent(PersistenceOperation operation, bool success, Exception error = null)
        {
            Operation = operation;
            Success = success;
            Error = error;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a string representation of the state persistence event.
        /// </summary>
        /// <returns>A string representation of the state persistence event.</returns>
        public override string ToString()
        {
            var status = Success ? "Success" : $"Failed: {Error?.Message}";
            return $"StatePersistence: {Operation} - {status} @ {Timestamp:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// Defines the types of persistence operations.
    /// </summary>
    public enum PersistenceOperation
    {
        /// <summary>
        /// State was saved to persistent storage.
        /// </summary>
        Save,

        /// <summary>
        /// State was loaded from persistent storage.
        /// </summary>
        Load,

        /// <summary>
        /// Persistent storage was cleared.
        /// </summary>
        Clear
    }
}
