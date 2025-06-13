using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// A deterministic scheduler for unit testing that allows manual time control.
    /// Enables predictable testing of time-based reactive operations and async sequences.
    /// </summary>
    public sealed class TestScheduler : IScheduler, IDisposable
    {
        private readonly object _lock = new object();
        private readonly List<ScheduledItem> _scheduledItems;
        private long _currentTime;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the TestScheduler class.
        /// </summary>
        public TestScheduler()
        {
            _scheduledItems = new List<ScheduledItem>();
            _currentTime = 0;
        }

        /// <summary>
        /// Gets the current virtual time according to the test scheduler.
        /// </summary>
        public DateTimeOffset Now => new DateTimeOffset(_currentTime, TimeSpan.Zero);

        /// <summary>
        /// Gets the current virtual time in ticks.
        /// </summary>
        public long CurrentTime
        {
            get
            {
                lock (_lock)
                {
                    return _currentTime;
                }
            }
        }

        /// <summary>
        /// Advances the virtual time by the specified amount.
        /// </summary>
        /// <param name="time">The amount of time to advance.</param>
        public void AdvanceBy(TimeSpan time)
        {
            if (time < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(time), "Time cannot be negative");

            AdvanceTo(_currentTime + time.Ticks);
        }

        /// <summary>
        /// Advances the virtual time to the specified time.
        /// </summary>
        /// <param name="time">The time to advance to.</param>
        public void AdvanceTo(long time)
        {
            CheckDisposed();

            lock (_lock)
            {
                if (time < _currentTime)
                    throw new ArgumentOutOfRangeException(nameof(time), "Cannot go back in time");

                _currentTime = time;
                ProcessScheduledItems();
            }
        }

        /// <summary>
        /// Advances the virtual time to the specified time.
        /// </summary>
        /// <param name="time">The time to advance to.</param>
        public void AdvanceTo(DateTimeOffset time)
        {
            AdvanceTo(time.Ticks);
        }

        /// <summary>
        /// Starts the scheduler and processes all scheduled items until there are none left.
        /// </summary>
        public void Start()
        {
            CheckDisposed();

            lock (_lock)
            {
                while (_scheduledItems.Count > 0)
                {
                    var nextItem = _scheduledItems.OrderBy(x => x.DueTime).First();
                    _currentTime = nextItem.DueTime;
                    ProcessScheduledItems();
                }
            }
        }

        /// <summary>
        /// Schedules an action to be executed.
        /// </summary>
        /// <typeparam name="TState">The type of the state passed to the scheduled action.</typeparam>
        /// <param name="state">State passed to the action to be executed.</param>
        /// <param name="action">Action to be executed.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            CheckDisposed();

            // Execute immediately for immediate scheduling
            try
            {
                action(this, state);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo.Reactive] Scheduled action threw exception: {ex}");
            }

            return Disposable.Empty;
        }

        /// <summary>
        /// Schedules an action to be executed after dueTime.
        /// </summary>
        /// <typeparam name="TState">The type of the state passed to the scheduled action.</typeparam>
        /// <param name="state">State passed to the action to be executed.</param>
        /// <param name="dueTime">Relative time after which to execute the action.</param>
        /// <param name="action">Action to be executed.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            CheckDisposed();

            // If dueTime is zero or negative, execute immediately
            if (dueTime <= TimeSpan.Zero)
            {
                try
                {
                    action(this, state);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Scheduled action threw exception: {ex}");
                }
                return Disposable.Empty;
            }

            var scheduledItem = new ScheduledItem<TState>
            {
                DueTime = _currentTime + dueTime.Ticks,
                State = state,
                Action = action,
                Scheduler = this
            };

            lock (_lock)
            {
                _scheduledItems.Add(scheduledItem);
            }

            return Disposable.Create(() =>
            {
                lock (_lock)
                {
                    _scheduledItems.Remove(scheduledItem);
                }
            });
        }

        /// <summary>
        /// Schedules a periodic piece of work.
        /// </summary>
        /// <typeparam name="TState">The type of the state passed to the scheduled action.</typeparam>
        /// <param name="state">Initial state passed to the action upon the first iteration.</param>
        /// <param name="period">Period for running the work periodically.</param>
        /// <param name="action">Action to be executed, potentially updating the state.</param>
        /// <returns>The disposable object used to cancel the scheduled recurring action (best effort).</returns>
        public IDisposable SchedulePeriodic<TState>(TState state, TimeSpan period, Func<TState, TState> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (period <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(period), "Period must be positive");

            CheckDisposed();

            var cancelled = false;
            var nextDueTime = _currentTime + period.Ticks;

            void ScheduleNext(TState currentState, long dueTime)
            {
                if (cancelled) return;

                var scheduledItem = new ScheduledItem<TState>
                {
                    DueTime = dueTime,
                    State = currentState,
                    Action = (scheduler, s) =>
                    {
                        if (cancelled) return Disposable.Empty;

                        try
                        {
                            var newState = action(s);
                            // Schedule next execution at the next period interval
                            ScheduleNext(newState, dueTime + period.Ticks);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[Ludo.Reactive] Periodic action threw exception: {ex}");
                        }

                        return Disposable.Empty;
                    },
                    Scheduler = this
                };

                lock (_lock)
                {
                    _scheduledItems.Add(scheduledItem);
                }
            }

            // Start the periodic scheduling
            ScheduleNext(state, nextDueTime);

            return Disposable.Create(() => cancelled = true);
        }

        /// <summary>
        /// Gets all scheduled items that are due at or before the current time.
        /// </summary>
        /// <returns>A list of scheduled items.</returns>
        public IReadOnlyList<ScheduledItem> GetScheduledItems()
        {
            lock (_lock)
            {
                return _scheduledItems.ToList();
            }
        }

        /// <summary>
        /// Clears all scheduled items.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _scheduledItems.Clear();
            }
        }

        private void ProcessScheduledItems()
        {
            // Keep processing items until no more items are due at the current time
            // This handles cases where executing an item schedules new items that are also due
            while (true)
            {
                var itemsToExecute = _scheduledItems
                    .Where(x => x.DueTime <= _currentTime)
                    .OrderBy(x => x.DueTime)
                    .ToList();

                if (itemsToExecute.Count == 0)
                    break;

                foreach (var item in itemsToExecute)
                {
                    _scheduledItems.Remove(item);

                    try
                    {
                        item.Execute();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Scheduled action threw exception: {ex}");
                    }
                }
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TestScheduler));
        }

        /// <summary>
        /// Releases all resources used by the TestScheduler.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _scheduledItems.Clear();
            }
        }
    }

    /// <summary>
    /// Represents a scheduled item in the TestScheduler.
    /// </summary>
    public abstract class ScheduledItem
    {
        /// <summary>
        /// Gets or sets the time when this item is due to execute.
        /// </summary>
        public long DueTime { get; set; }

        /// <summary>
        /// Executes the scheduled action.
        /// </summary>
        public abstract void Execute();
    }

    /// <summary>
    /// Represents a typed scheduled item in the TestScheduler.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    public class ScheduledItem<TState> : ScheduledItem
    {
        /// <summary>
        /// Gets or sets the state for the scheduled action.
        /// </summary>
        public TState State { get; set; }

        /// <summary>
        /// Gets or sets the action to execute.
        /// </summary>
        public Func<IScheduler, TState, IDisposable> Action { get; set; }

        /// <summary>
        /// Gets or sets the scheduler instance.
        /// </summary>
        public IScheduler Scheduler { get; set; }

        /// <summary>
        /// Executes the scheduled action.
        /// </summary>
        public override void Execute()
        {
            Action?.Invoke(Scheduler, State);
        }
    }
}
