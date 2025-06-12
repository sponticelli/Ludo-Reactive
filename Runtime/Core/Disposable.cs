using System;
using System.Collections.Generic;

namespace Ludo.Reactive
{
    /// <summary>
    /// Provides a set of static methods for creating Disposables.
    /// </summary>
    public static class Disposable
    {
        /// <summary>
        /// Gets the disposable that does nothing when disposed.
        /// </summary>
        public static readonly IDisposable Empty = new EmptyDisposable();

        /// <summary>
        /// Creates a disposable object that invokes the specified action when disposed.
        /// </summary>
        /// <param name="dispose">Action to run during the first call to Dispose. The action is guaranteed to be run at most once.</param>
        /// <returns>The disposable object that runs the given action upon disposal.</returns>
        public static IDisposable Create(Action dispose)
        {
            if (dispose == null)
                throw new ArgumentNullException(nameof(dispose));

            return new AnonymousDisposable(dispose);
        }

        /// <summary>
        /// Creates a disposable object that invokes the specified action when disposed.
        /// </summary>
        /// <typeparam name="TState">The type of the state passed to the dispose action.</typeparam>
        /// <param name="state">The state to be passed to the dispose action.</param>
        /// <param name="dispose">Action to run during the first call to Dispose. The action is guaranteed to be run at most once.</param>
        /// <returns>The disposable object that runs the given action upon disposal.</returns>
        public static IDisposable Create<TState>(TState state, Action<TState> dispose)
        {
            if (dispose == null)
                throw new ArgumentNullException(nameof(dispose));

            return new AnonymousDisposable<TState>(state, dispose);
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
                // Intentionally empty
            }
        }

        private sealed class AnonymousDisposable : IDisposable
        {
            private volatile Action _dispose;

            public AnonymousDisposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                var dispose = _dispose;
                if (dispose != null)
                {
                    _dispose = null;
                    dispose();
                }
            }
        }

        private sealed class AnonymousDisposable<TState> : IDisposable
        {
            private volatile Action<TState> _dispose;
            private readonly TState _state;

            public AnonymousDisposable(TState state, Action<TState> dispose)
            {
                _state = state;
                _dispose = dispose;
            }

            public void Dispose()
            {
                var dispose = _dispose;
                if (dispose != null)
                {
                    _dispose = null;
                    dispose(_state);
                }
            }
        }
    }

    /// <summary>
    /// Represents a group of disposable resources that are disposed together.
    /// </summary>
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly object _lock = new object();
        private List<IDisposable> _disposables;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the CompositeDisposable class with no disposables contained by it initially.
        /// </summary>
        public CompositeDisposable()
        {
            _disposables = new List<IDisposable>();
        }

        /// <summary>
        /// Initializes a new instance of the CompositeDisposable class with the specified number of disposables.
        /// </summary>
        /// <param name="capacity">The number of disposables that the new CompositeDisposable can initially store.</param>
        public CompositeDisposable(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _disposables = new List<IDisposable>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the CompositeDisposable class from a group of disposables.
        /// </summary>
        /// <param name="disposables">Disposables that will be disposed together.</param>
        public CompositeDisposable(params IDisposable[] disposables)
        {
            if (disposables == null)
                throw new ArgumentNullException(nameof(disposables));

            _disposables = new List<IDisposable>(disposables);
        }

        /// <summary>
        /// Gets the number of disposables contained in the CompositeDisposable.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _disposed ? 0 : _disposables.Count;
                }
            }
        }

        /// <summary>
        /// Adds a disposable to the CompositeDisposable or disposes the disposable if the CompositeDisposable is disposed.
        /// </summary>
        /// <param name="item">Disposable to add.</param>
        public void Add(IDisposable item)
        {
            if (item == null)
                return;

            bool shouldDispose = false;
            lock (_lock)
            {
                if (_disposed)
                {
                    shouldDispose = true;
                }
                else
                {
                    _disposables.Add(item);
                }
            }

            if (shouldDispose)
                item.Dispose();
        }

        /// <summary>
        /// Removes and disposes the first occurrence of a disposable from the CompositeDisposable.
        /// </summary>
        /// <param name="item">Disposable to remove.</param>
        /// <returns>true if found; false otherwise.</returns>
        public bool Remove(IDisposable item)
        {
            if (item == null)
                return false;

            bool shouldDispose = false;

            lock (_lock)
            {
                if (!_disposed)
                {
                    var index = _disposables.IndexOf(item);
                    if (index >= 0)
                    {
                        shouldDispose = true;
                        _disposables.RemoveAt(index);
                    }
                }
            }

            if (shouldDispose)
                item.Dispose();

            return shouldDispose;
        }

        /// <summary>
        /// Disposes all disposables in the group and removes them from the group.
        /// </summary>
        public void Dispose()
        {
            List<IDisposable> currentDisposables = null;

            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    currentDisposables = _disposables;
                    _disposables = null;
                }
            }

            if (currentDisposables != null)
            {
                foreach (var disposable in currentDisposables)
                {
                    disposable?.Dispose();
                }
            }
        }

        /// <summary>
        /// Removes and disposes all disposables from the CompositeDisposable, but does not dispose the CompositeDisposable.
        /// </summary>
        public void Clear()
        {
            List<IDisposable> currentDisposables = null;

            lock (_lock)
            {
                if (!_disposed)
                {
                    currentDisposables = _disposables;
                    _disposables = new List<IDisposable>();
                }
            }

            if (currentDisposables != null)
            {
                foreach (var disposable in currentDisposables)
                {
                    disposable?.Dispose();
                }
            }
        }
    }
}
