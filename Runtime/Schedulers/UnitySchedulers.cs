using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// Provides Unity-specific schedulers for different execution contexts.
    /// </summary>
    public static class UnitySchedulers
    {
        private static IScheduler _mainThread;
        private static IScheduler _fixedUpdate;
        private static IScheduler _lateUpdate;
        private static IScheduler _endOfFrame;

        /// <summary>
        /// Gets a scheduler that schedules work on Unity's main thread.
        /// </summary>
        public static IScheduler MainThread
        {
            get
            {
                if (_mainThread == null)
                    _mainThread = new MainThreadScheduler();
                return _mainThread;
            }
        }

        /// <summary>
        /// Gets a scheduler that schedules work during Unity's FixedUpdate.
        /// </summary>
        public static IScheduler FixedUpdate
        {
            get
            {
                if (_fixedUpdate == null)
                    _fixedUpdate = new FixedUpdateScheduler();
                return _fixedUpdate;
            }
        }

        /// <summary>
        /// Gets a scheduler that schedules work during Unity's LateUpdate.
        /// </summary>
        public static IScheduler LateUpdate
        {
            get
            {
                if (_lateUpdate == null)
                    _lateUpdate = new LateUpdateScheduler();
                return _lateUpdate;
            }
        }

        /// <summary>
        /// Gets a scheduler that schedules work at the end of the frame.
        /// </summary>
        public static IScheduler EndOfFrame
        {
            get
            {
                if (_endOfFrame == null)
                    _endOfFrame = new EndOfFrameScheduler();
                return _endOfFrame;
            }
        }

        /// <summary>
        /// Creates a new TestScheduler for deterministic testing.
        /// </summary>
        /// <returns>A new TestScheduler instance.</returns>
        public static TestScheduler CreateTestScheduler()
        {
            return new TestScheduler();
        }
    }

    /// <summary>
    /// Base class for Unity schedulers.
    /// </summary>
    public abstract class UnitySchedulerBase : IScheduler
    {
        private static SchedulerRunner _runner;

        internal static SchedulerRunner Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("Ludo.Reactive.SchedulerRunner");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _runner = go.AddComponent<SchedulerRunner>();
                }
                return _runner;
            }
        }

        public DateTimeOffset Now => DateTimeOffset.UtcNow;

        public abstract IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action);

        public virtual IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            if (dueTime <= TimeSpan.Zero)
                return Schedule(state, action);

            var composite = new CompositeDisposable();
            var cancelled = false;

            composite.Add(Disposable.Create(() => cancelled = true));

            Runner.StartCoroutine(DelayedExecution());

            return composite;

            IEnumerator DelayedExecution()
            {
                yield return new WaitForSeconds((float)dueTime.TotalSeconds);
                
                if (!cancelled)
                {
                    var disposable = Schedule(state, action);
                    composite.Add(disposable);
                }
            }
        }

        public virtual IDisposable SchedulePeriodic<TState>(TState state, TimeSpan period, Func<TState, TState> action)
        {
            var composite = new CompositeDisposable();
            var cancelled = false;
            var currentState = state;

            composite.Add(Disposable.Create(() => cancelled = true));

            Runner.StartCoroutine(PeriodicExecution());

            return composite;

            IEnumerator PeriodicExecution()
            {
                while (!cancelled)
                {
                    yield return new WaitForSeconds((float)period.TotalSeconds);
                    
                    if (!cancelled)
                    {
                        try
                        {
                            currentState = action(currentState);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[Ludo.Reactive] Periodic action threw exception: {ex}");
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Scheduler that executes work on Unity's main thread.
    /// </summary>
    internal sealed class MainThreadScheduler : UnitySchedulerBase
    {
        public override IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            var composite = new CompositeDisposable();
            var cancelled = false;

            composite.Add(Disposable.Create(() => cancelled = true));

            Runner.ScheduleOnUpdate(() =>
            {
                if (!cancelled)
                {
                    try
                    {
                        var disposable = action(this, state);
                        composite.Add(disposable);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Scheduled action threw exception: {ex}");
                    }
                }
            });

            return composite;
        }
    }

    /// <summary>
    /// Scheduler that executes work during Unity's FixedUpdate.
    /// </summary>
    internal sealed class FixedUpdateScheduler : UnitySchedulerBase
    {
        public override IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            var composite = new CompositeDisposable();
            var cancelled = false;

            composite.Add(Disposable.Create(() => cancelled = true));

            Runner.ScheduleOnFixedUpdate(() =>
            {
                if (!cancelled)
                {
                    try
                    {
                        var disposable = action(this, state);
                        composite.Add(disposable);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Scheduled action threw exception: {ex}");
                    }
                }
            });

            return composite;
        }
    }

    /// <summary>
    /// Scheduler that executes work during Unity's LateUpdate.
    /// </summary>
    internal sealed class LateUpdateScheduler : UnitySchedulerBase
    {
        public override IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            var composite = new CompositeDisposable();
            var cancelled = false;

            composite.Add(Disposable.Create(() => cancelled = true));

            Runner.ScheduleOnLateUpdate(() =>
            {
                if (!cancelled)
                {
                    try
                    {
                        var disposable = action(this, state);
                        composite.Add(disposable);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Scheduled action threw exception: {ex}");
                    }
                }
            });

            return composite;
        }
    }

    /// <summary>
    /// Scheduler that executes work at the end of the frame.
    /// </summary>
    internal sealed class EndOfFrameScheduler : UnitySchedulerBase
    {
        public override IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            var composite = new CompositeDisposable();
            var cancelled = false;

            composite.Add(Disposable.Create(() => cancelled = true));

            Runner.ScheduleOnEndOfFrame(() =>
            {
                if (!cancelled)
                {
                    try
                    {
                        var disposable = action(this, state);
                        composite.Add(disposable);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ludo.Reactive] Scheduled action threw exception: {ex}");
                    }
                }
            });

            return composite;
        }
    }
}
