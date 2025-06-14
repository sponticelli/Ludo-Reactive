using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ludo.Reactive.StateManagement
{
    /// <summary>
    /// Represents a command that can be executed and undone.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public interface IReversibleCommand<TState>
    {
        /// <summary>
        /// Gets the name of this command for debugging purposes.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the timestamp when this command was created.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Executes the command and returns the new state.
        /// </summary>
        /// <param name="currentState">The current state.</param>
        /// <returns>The new state after executing the command.</returns>
        TState Execute(TState currentState);

        /// <summary>
        /// Undoes the command and returns the previous state.
        /// </summary>
        /// <param name="currentState">The current state.</param>
        /// <returns>The state before the command was executed.</returns>
        TState Undo(TState currentState);

        /// <summary>
        /// Gets whether this command can be undone.
        /// </summary>
        bool CanUndo { get; }
    }

    /// <summary>
    /// Base implementation of IReversibleCommand providing common functionality.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public abstract class ReversibleCommandBase<TState> : IReversibleCommand<TState>
    {
        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public DateTime Timestamp { get; }

        /// <inheritdoc />
        public virtual bool CanUndo => true;

        /// <summary>
        /// Initializes a new instance of the ReversibleCommandBase class.
        /// </summary>
        protected ReversibleCommandBase()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public abstract TState Execute(TState currentState);

        /// <inheritdoc />
        public abstract TState Undo(TState currentState);

        /// <summary>
        /// Returns a string representation of this command.
        /// </summary>
        /// <returns>A string representation of this command.</returns>
        public override string ToString()
        {
            return $"{Name} @ {Timestamp:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// Manages a history of reversible commands for undo/redo functionality.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public class CommandHistory<TState> : IDisposable
    {
        private readonly List<IReversibleCommand<TState>> _history;
        private readonly Subject<CommandHistoryEvent<TState>> _historyChangedSubject;
        private readonly int _maxHistorySize;
        private int _currentIndex;
        private bool _disposed;

        /// <summary>
        /// Gets the maximum number of commands to keep in history.
        /// </summary>
        public int MaxHistorySize => _maxHistorySize;

        /// <summary>
        /// Gets the current number of commands in history.
        /// </summary>
        public int Count => _history.Count;

        /// <summary>
        /// Gets the current position in the history.
        /// </summary>
        public int CurrentIndex => _currentIndex;

        /// <summary>
        /// Gets whether there are commands that can be undone.
        /// </summary>
        public bool CanUndo => _currentIndex > 0 && _history.Count > 0;

        /// <summary>
        /// Gets whether there are commands that can be redone.
        /// </summary>
        public bool CanRedo => _currentIndex < _history.Count;

        /// <summary>
        /// Gets an observable stream of history change events.
        /// </summary>
        public IObservable<CommandHistoryEvent<TState>> HistoryChanged => _historyChangedSubject;

        /// <summary>
        /// Initializes a new instance of the CommandHistory class.
        /// </summary>
        /// <param name="maxHistorySize">The maximum number of commands to keep in history.</param>
        public CommandHistory(int maxHistorySize = 100)
        {
            if (maxHistorySize <= 0)
                throw new ArgumentException("Max history size must be greater than zero", nameof(maxHistorySize));

            _maxHistorySize = maxHistorySize;
            _history = new List<IReversibleCommand<TState>>();
            _historyChangedSubject = new Subject<CommandHistoryEvent<TState>>();
            _currentIndex = 0;
        }

        /// <summary>
        /// Executes a command and adds it to the history.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="currentState">The current state.</param>
        /// <returns>The new state after executing the command.</returns>
        public TState ExecuteCommand(IReversibleCommand<TState> command, TState currentState)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            CheckDisposed();

            try
            {
                var newState = command.Execute(currentState);

                // Remove any commands after the current index (for branching undo/redo)
                if (_currentIndex < _history.Count)
                {
                    _history.RemoveRange(_currentIndex, _history.Count - _currentIndex);
                }

                // Add the new command
                _history.Add(command);
                _currentIndex = _history.Count;

                // Trim history if it exceeds max size
                TrimHistory();

                // Notify observers
                var historyEvent = new CommandHistoryEvent<TState>(
                    CommandHistoryOperation.Execute,
                    command,
                    _currentIndex,
                    CanUndo,
                    CanRedo
                );
                _historyChangedSubject.OnNext(historyEvent);

                return newState;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Failed to execute command '{command.Name}': {ex}");
                throw;
            }
        }

        /// <summary>
        /// Undoes the last command and returns the previous state.
        /// </summary>
        /// <param name="currentState">The current state.</param>
        /// <returns>The state before the last command was executed.</returns>
        public TState Undo(TState currentState)
        {
            CheckDisposed();

            if (!CanUndo)
            {
                Debug.LogWarning("[Ludo.Reactive] Cannot undo: no commands in history or already at beginning");
                return currentState;
            }

            try
            {
                var command = _history[_currentIndex - 1];
                if (!command.CanUndo)
                {
                    Debug.LogWarning($"[Ludo.Reactive] Cannot undo command '{command.Name}': command is not undoable");
                    return currentState;
                }

                var newState = command.Undo(currentState);
                _currentIndex--;

                // Notify observers
                var historyEvent = new CommandHistoryEvent<TState>(
                    CommandHistoryOperation.Undo,
                    command,
                    _currentIndex,
                    CanUndo,
                    CanRedo
                );
                _historyChangedSubject.OnNext(historyEvent);

                return newState;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Failed to undo command: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Redoes the next command and returns the new state.
        /// </summary>
        /// <param name="currentState">The current state.</param>
        /// <returns>The state after redoing the command.</returns>
        public TState Redo(TState currentState)
        {
            CheckDisposed();

            if (!CanRedo)
            {
                Debug.LogWarning("[Ludo.Reactive] Cannot redo: no commands to redo");
                return currentState;
            }

            try
            {
                var command = _history[_currentIndex];
                var newState = command.Execute(currentState);
                _currentIndex++;

                // Notify observers
                var historyEvent = new CommandHistoryEvent<TState>(
                    CommandHistoryOperation.Redo,
                    command,
                    _currentIndex,
                    CanUndo,
                    CanRedo
                );
                _historyChangedSubject.OnNext(historyEvent);

                return newState;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Failed to redo command: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Clears the command history.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();

            _history.Clear();
            _currentIndex = 0;

            var historyEvent = new CommandHistoryEvent<TState>(
                CommandHistoryOperation.Clear,
                null,
                _currentIndex,
                CanUndo,
                CanRedo
            );
            _historyChangedSubject.OnNext(historyEvent);
        }

        /// <summary>
        /// Gets a read-only view of the command history.
        /// </summary>
        /// <returns>A read-only list of commands in history.</returns>
        public IReadOnlyList<IReversibleCommand<TState>> GetHistory()
        {
            CheckDisposed();
            return _history.AsReadOnly();
        }

        /// <summary>
        /// Gets the command at the specified index.
        /// </summary>
        /// <param name="index">The index of the command.</param>
        /// <returns>The command at the specified index.</returns>
        public IReversibleCommand<TState> GetCommand(int index)
        {
            CheckDisposed();

            if (index < 0 || index >= _history.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _history[index];
        }

        private void TrimHistory()
        {
            while (_history.Count > _maxHistorySize)
            {
                _history.RemoveAt(0);
                _currentIndex--;
            }

            // Ensure current index is valid
            _currentIndex = Math.Max(0, Math.Min(_currentIndex, _history.Count));
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CommandHistory<TState>));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _history.Clear();
            _historyChangedSubject?.Dispose();
        }
    }

    /// <summary>
    /// Represents an event that occurs when the command history changes.
    /// </summary>
    /// <typeparam name="TState">The type of the application state.</typeparam>
    public struct CommandHistoryEvent<TState>
    {
        /// <summary>
        /// Gets the type of operation that occurred.
        /// </summary>
        public CommandHistoryOperation Operation { get; }

        /// <summary>
        /// Gets the command involved in the operation.
        /// </summary>
        public IReversibleCommand<TState> Command { get; }

        /// <summary>
        /// Gets the current index in the history.
        /// </summary>
        public int CurrentIndex { get; }

        /// <summary>
        /// Gets whether undo is available.
        /// </summary>
        public bool CanUndo { get; }

        /// <summary>
        /// Gets whether redo is available.
        /// </summary>
        public bool CanRedo { get; }

        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the CommandHistoryEvent struct.
        /// </summary>
        /// <param name="operation">The type of operation.</param>
        /// <param name="command">The command involved.</param>
        /// <param name="currentIndex">The current index.</param>
        /// <param name="canUndo">Whether undo is available.</param>
        /// <param name="canRedo">Whether redo is available.</param>
        public CommandHistoryEvent(
            CommandHistoryOperation operation,
            IReversibleCommand<TState> command,
            int currentIndex,
            bool canUndo,
            bool canRedo)
        {
            Operation = operation;
            Command = command;
            CurrentIndex = currentIndex;
            CanUndo = canUndo;
            CanRedo = canRedo;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Defines the types of command history operations.
    /// </summary>
    public enum CommandHistoryOperation
    {
        /// <summary>
        /// A command was executed.
        /// </summary>
        Execute,

        /// <summary>
        /// A command was undone.
        /// </summary>
        Undo,

        /// <summary>
        /// A command was redone.
        /// </summary>
        Redo,

        /// <summary>
        /// The history was cleared.
        /// </summary>
        Clear
    }
}
