using System;

namespace Ludo.Reactive.StateManagement
{
    /// <summary>
    /// Represents a pure function that takes the current state and an action,
    /// and returns a new state. Reducers specify how the application's state
    /// changes in response to actions.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    /// <typeparam name="TAction">The type of actions this reducer handles.</typeparam>
    public interface IReducer<TState, in TAction> where TAction : IAction
    {
        /// <summary>
        /// Reduces the current state and action to a new state.
        /// This method must be pure (no side effects) and deterministic.
        /// </summary>
        /// <param name="currentState">The current state.</param>
        /// <param name="action">The action to apply.</param>
        /// <returns>The new state after applying the action.</returns>
        TState Reduce(TState currentState, TAction action);
        
        /// <summary>
        /// Determines whether this reducer can handle the specified action type.
        /// </summary>
        /// <param name="action">The action to check.</param>
        /// <returns>True if this reducer can handle the action; otherwise, false.</returns>
        bool CanHandle(IAction action);
    }

    /// <summary>
    /// Base implementation of IReducer providing common functionality.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    /// <typeparam name="TAction">The type of actions this reducer handles.</typeparam>
    public abstract class ReducerBase<TState, TAction> : IReducer<TState, TAction> 
        where TAction : class, IAction
    {
        /// <inheritdoc />
        public abstract TState Reduce(TState currentState, TAction action);

        /// <inheritdoc />
        public virtual bool CanHandle(IAction action)
        {
            return action is TAction;
        }
    }

    /// <summary>
    /// A composite reducer that combines multiple reducers into one.
    /// Each sub-reducer handles a specific slice of the state.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class CompositeReducer<TState> : IReducer<TState, IAction>
    {
        private readonly IReducer<TState, IAction>[] _reducers;

        /// <summary>
        /// Initializes a new instance of the CompositeReducer class.
        /// </summary>
        /// <param name="reducers">The reducers to combine.</param>
        public CompositeReducer(params IReducer<TState, IAction>[] reducers)
        {
            _reducers = reducers ?? throw new ArgumentNullException(nameof(reducers));
        }

        /// <inheritdoc />
        public TState Reduce(TState currentState, IAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var newState = currentState;
            
            foreach (var reducer in _reducers)
            {
                if (reducer.CanHandle(action))
                {
                    newState = reducer.Reduce(newState, action);
                }
            }

            return newState;
        }

        /// <inheritdoc />
        public bool CanHandle(IAction action)
        {
            foreach (var reducer in _reducers)
            {
                if (reducer.CanHandle(action))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// A functional reducer that uses delegates for state reduction.
    /// Useful for simple reducers that don't require a full class implementation.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    /// <typeparam name="TAction">The type of actions this reducer handles.</typeparam>
    public class FunctionalReducer<TState, TAction> : IReducer<TState, TAction> 
        where TAction : class, IAction
    {
        private readonly Func<TState, TAction, TState> _reduceFunc;
        private readonly Func<IAction, bool> _canHandleFunc;

        /// <summary>
        /// Initializes a new instance of the FunctionalReducer class.
        /// </summary>
        /// <param name="reduceFunc">The function to use for state reduction.</param>
        /// <param name="canHandleFunc">Optional function to determine if action can be handled.</param>
        public FunctionalReducer(
            Func<TState, TAction, TState> reduceFunc,
            Func<IAction, bool> canHandleFunc = null)
        {
            _reduceFunc = reduceFunc ?? throw new ArgumentNullException(nameof(reduceFunc));
            _canHandleFunc = canHandleFunc ?? (action => action is TAction);
        }

        /// <inheritdoc />
        public TState Reduce(TState currentState, TAction action)
        {
            return _reduceFunc(currentState, action);
        }

        /// <inheritdoc />
        public bool CanHandle(IAction action)
        {
            return _canHandleFunc(action);
        }
    }
}
