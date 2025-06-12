using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents an object that schedules units of work.
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Gets the current time according to the local machine's system clock.
        /// </summary>
        DateTimeOffset Now { get; }

        /// <summary>
        /// Schedules an action to be executed.
        /// </summary>
        /// <typeparam name="TState">The type of the state passed to the scheduled action.</typeparam>
        /// <param name="state">State passed to the action to be executed.</param>
        /// <param name="action">Action to be executed.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action);

        /// <summary>
        /// Schedules an action to be executed after dueTime.
        /// </summary>
        /// <typeparam name="TState">The type of the state passed to the scheduled action.</typeparam>
        /// <param name="state">State passed to the action to be executed.</param>
        /// <param name="dueTime">Relative time after which to execute the action.</param>
        /// <param name="action">Action to be executed.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action);

        /// <summary>
        /// Schedules a periodic piece of work.
        /// </summary>
        /// <typeparam name="TState">The type of the state passed to the scheduled action.</typeparam>
        /// <param name="state">Initial state passed to the action upon the first iteration.</param>
        /// <param name="period">Period for running the work periodically.</param>
        /// <param name="action">Action to be executed, potentially updating the state.</param>
        /// <returns>The disposable object used to cancel the scheduled recurring action (best effort).</returns>
        IDisposable SchedulePeriodic<TState>(TState state, TimeSpan period, Func<TState, TState> action);
    }

    /// <summary>
    /// Provides extension methods for IScheduler.
    /// </summary>
    public static class SchedulerExtensions
    {
        /// <summary>
        /// Schedules an action to be executed.
        /// </summary>
        /// <param name="scheduler">Scheduler to execute the action on.</param>
        /// <param name="action">Action to be executed.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        public static IDisposable Schedule(this IScheduler scheduler, Action action)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return scheduler.Schedule(action, (s, a) =>
            {
                a();
                return Disposable.Empty;
            });
        }

        /// <summary>
        /// Schedules an action to be executed after dueTime.
        /// </summary>
        /// <param name="scheduler">Scheduler to execute the action on.</param>
        /// <param name="dueTime">Relative time after which to execute the action.</param>
        /// <param name="action">Action to be executed.</param>
        /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
        public static IDisposable Schedule(this IScheduler scheduler, TimeSpan dueTime, Action action)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return scheduler.Schedule(action, dueTime, (s, a) =>
            {
                a();
                return Disposable.Empty;
            });
        }

        /// <summary>
        /// Schedules a periodic piece of work.
        /// </summary>
        /// <param name="scheduler">Scheduler to execute the action on.</param>
        /// <param name="period">Period for running the work periodically.</param>
        /// <param name="action">Action to be executed.</param>
        /// <returns>The disposable object used to cancel the scheduled recurring action (best effort).</returns>
        public static IDisposable SchedulePeriodic(this IScheduler scheduler, TimeSpan period, Action action)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return scheduler.SchedulePeriodic(Unit.Default, period, _ =>
            {
                action();
                return Unit.Default;
            });
        }
    }
}
