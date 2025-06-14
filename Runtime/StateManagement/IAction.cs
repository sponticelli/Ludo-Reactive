using System;

namespace Ludo.Reactive.StateManagement
{
    /// <summary>
    /// Represents an action that can be dispatched to modify application state.
    /// Actions are immutable objects that describe what happened in the application.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Gets the unique type identifier for this action.
        /// Used for action routing and debugging.
        /// </summary>
        string Type { get; }
        
        /// <summary>
        /// Gets the timestamp when this action was created.
        /// </summary>
        DateTime Timestamp { get; }
    }

    /// <summary>
    /// Base implementation of IAction providing common functionality.
    /// </summary>
    [Serializable]
    public abstract class ActionBase : IAction
    {
        /// <inheritdoc />
        public abstract string Type { get; }
        
        /// <inheritdoc />
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the ActionBase class.
        /// </summary>
        protected ActionBase()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a string representation of this action.
        /// </summary>
        /// <returns>A string representation of this action.</returns>
        public override string ToString()
        {
            return $"{Type} @ {Timestamp:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// Represents an action that can be undone.
    /// Used for implementing undo/redo functionality.
    /// </summary>
    public interface IReversibleAction : IAction
    {
        /// <summary>
        /// Creates the inverse action that can undo this action's effects.
        /// </summary>
        /// <typeparam name="TState">The type of the application state.</typeparam>
        /// <param name="currentState">The current state before applying this action.</param>
        /// <returns>An action that can undo this action's effects.</returns>
        IAction CreateInverse<TState>(TState currentState);
    }

    /// <summary>
    /// Represents an action that carries a payload of data.
    /// </summary>
    /// <typeparam name="TPayload">The type of the payload.</typeparam>
    public interface IPayloadAction<out TPayload> : IAction
    {
        /// <summary>
        /// Gets the payload data for this action.
        /// </summary>
        TPayload Payload { get; }
    }

    /// <summary>
    /// Base implementation for actions with payload.
    /// </summary>
    /// <typeparam name="TPayload">The type of the payload.</typeparam>
    [Serializable]
    public abstract class PayloadActionBase<TPayload> : ActionBase, IPayloadAction<TPayload>
    {
        /// <inheritdoc />
        public TPayload Payload { get; }

        /// <summary>
        /// Initializes a new instance of the PayloadActionBase class.
        /// </summary>
        /// <param name="payload">The payload data for this action.</param>
        protected PayloadActionBase(TPayload payload)
        {
            Payload = payload;
        }

        /// <summary>
        /// Returns a string representation of this action including payload information.
        /// </summary>
        /// <returns>A string representation of this action.</returns>
        public override string ToString()
        {
            return $"{Type}({Payload}) @ {Timestamp:HH:mm:ss.fff}";
        }
    }
}
