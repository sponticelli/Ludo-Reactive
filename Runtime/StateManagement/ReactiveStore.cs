using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive.StateManagement
{
    /// <summary>
    /// A reactive store that manages application state using the Redux pattern.
    /// Provides immutable state updates, action dispatching, and reactive state observation.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class ReactiveStore<TState> : IObservable<TState>, IDisposable
    {
        private readonly IReducer<TState, IAction> _reducer;
        private readonly BehaviorSubject<TState> _stateSubject;
        private readonly Subject<StateChangedEvent<TState>> _stateChangedSubject;
        private readonly Subject<ActionDispatchedEvent> _actionDispatchedSubject;
        private readonly object _lock = new object();
        private readonly List<IMiddleware<TState>> _middlewares;
        
        private TState _currentState;
        private bool _disposed;

        /// <summary>
        /// Gets the current state of the store.
        /// </summary>
        public TState CurrentState
        {
            get
            {
                lock (_lock)
                {
                    CheckDisposed();
                    return _currentState;
                }
            }
        }

        /// <summary>
        /// Gets an observable stream of state changes.
        /// </summary>
        public IObservable<StateChangedEvent<TState>> StateChanged => _stateChangedSubject;

        /// <summary>
        /// Gets an observable stream of dispatched actions.
        /// </summary>
        public IObservable<ActionDispatchedEvent> ActionDispatched => _actionDispatchedSubject;

        /// <summary>
        /// Gets whether the store has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Initializes a new instance of the ReactiveStore class.
        /// </summary>
        /// <param name="initialState">The initial state of the store.</param>
        /// <param name="reducer">The reducer to handle state changes.</param>
        /// <param name="middlewares">Optional middlewares to apply.</param>
        public ReactiveStore(
            TState initialState,
            IReducer<TState, IAction> reducer,
            params IMiddleware<TState>[] middlewares)
        {
            _currentState = initialState;
            _reducer = reducer ?? throw new ArgumentNullException(nameof(reducer));
            _middlewares = new List<IMiddleware<TState>>(middlewares ?? Array.Empty<IMiddleware<TState>>());

            _stateSubject = new BehaviorSubject<TState>(_currentState);
            _stateChangedSubject = new Subject<StateChangedEvent<TState>>();
            _actionDispatchedSubject = new Subject<ActionDispatchedEvent>();
        }

        /// <summary>
        /// Dispatches an action to the store, potentially changing the state.
        /// </summary>
        /// <param name="action">The action to dispatch.</param>
        /// <returns>The new state after applying the action.</returns>
        public TState Dispatch(IAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            CheckDisposed();

            lock (_lock)
            {
                var previousState = _currentState;
                Exception error = null;
                bool success = false;

                try
                {
                    // Apply middlewares before reduction
                    var processedAction = ApplyMiddlewares(action, _currentState);
                    
                    // Apply reducer
                    var newState = _reducer.Reduce(_currentState, processedAction);
                    
                    // Update state if it changed
                    if (!EqualityComparer<TState>.Default.Equals(_currentState, newState))
                    {
                        _currentState = newState;
                        
                        // Emit state change events
                        var stateChangedEvent = new StateChangedEvent<TState>(previousState, newState, processedAction);
                        _stateChangedSubject.OnNext(stateChangedEvent);
                        _stateSubject.OnNext(newState);
                    }

                    success = true;
                }
                catch (Exception ex)
                {
                    error = ex;
                    Debug.LogError($"[Ludo.Reactive] Error dispatching action {action}: {ex}");
                }
                finally
                {
                    // Always emit action dispatched event
                    var actionEvent = new ActionDispatchedEvent(action, success, error);
                    _actionDispatchedSubject.OnNext(actionEvent);
                }

                return _currentState;
            }
        }

        /// <summary>
        /// Creates a reactive selector for a portion of the state.
        /// </summary>
        /// <typeparam name="TResult">The type of the selected result.</typeparam>
        /// <param name="selector">The selector function.</param>
        /// <param name="name">The name of the selector.</param>
        /// <returns>A reactive selector.</returns>
        public ReactiveSelector<TState, TResult> Select<TResult>(
            Func<TState, TResult> selector,
            string name = null)
        {
            CheckDisposed();
            return new ReactiveSelector<TState, TResult>(this, selector, name);
        }

        /// <summary>
        /// Adds a middleware to the store.
        /// </summary>
        /// <param name="middleware">The middleware to add.</param>
        public void AddMiddleware(IMiddleware<TState> middleware)
        {
            if (middleware == null)
                throw new ArgumentNullException(nameof(middleware));

            CheckDisposed();

            lock (_lock)
            {
                _middlewares.Add(middleware);
            }
        }

        /// <summary>
        /// Removes a middleware from the store.
        /// </summary>
        /// <param name="middleware">The middleware to remove.</param>
        /// <returns>True if the middleware was removed; otherwise, false.</returns>
        public bool RemoveMiddleware(IMiddleware<TState> middleware)
        {
            if (middleware == null)
                return false;

            CheckDisposed();

            lock (_lock)
            {
                return _middlewares.Remove(middleware);
            }
        }

        private IAction ApplyMiddlewares(IAction action, TState state)
        {
            var processedAction = action;
            
            foreach (var middleware in _middlewares)
            {
                try
                {
                    processedAction = middleware.Process(processedAction, state, this);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Middleware {middleware.GetType().Name} threw exception: {ex}");
                }
            }

            return processedAction;
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<TState> observer)
        {
            CheckDisposed();
            return _stateSubject.Subscribe(observer);
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

                try
                {
                    _stateSubject?.Dispose();
                    _stateChangedSubject?.Dispose();
                    _actionDispatchedSubject?.Dispose();

                    // Dispose middlewares if they implement IDisposable
                    foreach (var middleware in _middlewares)
                    {
                        if (middleware is IDisposable disposableMiddleware)
                        {
                            disposableMiddleware.Dispose();
                        }
                    }
                    _middlewares.Clear();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Error disposing ReactiveStore: {ex}");
                }
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ReactiveStore<TState>));
        }
    }

    /// <summary>
    /// Represents middleware that can intercept and modify actions before they reach the reducer.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public interface IMiddleware<TState>
    {
        /// <summary>
        /// Processes an action before it reaches the reducer.
        /// </summary>
        /// <param name="action">The action to process.</param>
        /// <param name="currentState">The current state.</param>
        /// <param name="store">The store instance.</param>
        /// <returns>The processed action (may be the same or a modified action).</returns>
        IAction Process(IAction action, TState currentState, ReactiveStore<TState> store);
    }

    /// <summary>
    /// A logging middleware that logs all dispatched actions.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class LoggingMiddleware<TState> : IMiddleware<TState>
    {
        private readonly bool _logState;

        /// <summary>
        /// Initializes a new instance of the LoggingMiddleware class.
        /// </summary>
        /// <param name="logState">Whether to log state changes as well as actions.</param>
        public LoggingMiddleware(bool logState = false)
        {
            _logState = logState;
        }

        /// <inheritdoc />
        public IAction Process(IAction action, TState currentState, ReactiveStore<TState> store)
        {
            Debug.Log($"[ReactiveStore] Action: {action}");
            
            if (_logState)
            {
                Debug.Log($"[ReactiveStore] Current State: {currentState}");
            }

            return action;
        }
    }
}
