using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// Provides a set of static methods for creating observable sequences.
    /// </summary>
    public static class Observable
    {
        /// <summary>
        /// Returns an observable sequence that contains a single element.
        /// </summary>
        /// <typeparam name="T">The type of the element in the resulting sequence.</typeparam>
        /// <param name="value">Single element in the resulting observable sequence.</param>
        /// <returns>An observable sequence containing the single specified element.</returns>
        public static IObservable<T> Return<T>(T value)
        {
            return Create<T>(observer =>
            {
                observer.OnNext(value);
                observer.OnCompleted();
                return Disposable.Empty;
            });
        }

        /// <summary>
        /// Returns an empty observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <returns>An observable sequence with no elements.</returns>
        public static IObservable<T> Empty<T>()
        {
            return Create<T>(observer =>
            {
                observer.OnCompleted();
                return Disposable.Empty;
            });
        }

        /// <summary>
        /// Returns a non-terminating observable sequence, which can be used to denote an infinite duration.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <returns>An observable sequence whose observers will never get called.</returns>
        public static IObservable<T> Never<T>()
        {
            return Create<T>(observer => Disposable.Empty);
        }

        /// <summary>
        /// Returns an observable sequence that terminates with an exception.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <param name="exception">Exception object used for the sequence's termination.</param>
        /// <returns>The observable sequence that terminates exceptionally with the specified exception object.</returns>
        public static IObservable<T> Throw<T>(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return Create<T>(observer =>
            {
                observer.OnError(exception);
                return Disposable.Empty;
            });
        }

        /// <summary>
        /// Creates an observable sequence from a specified Subscribe method implementation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <param name="subscribe">Implementation of the resulting observable sequence's Subscribe method.</param>
        /// <returns>The observable sequence with the specified implementation for the Subscribe method.</returns>
        public static IObservable<T> Create<T>(Func<IObserver<T>, IDisposable> subscribe)
        {
            if (subscribe == null)
                throw new ArgumentNullException(nameof(subscribe));

            return new AnonymousObservable<T>(subscribe);
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after each period.
        /// </summary>
        /// <param name="period">Period for producing the values in the resulting sequence.</param>
        /// <returns>An observable sequence that produces a value after each period.</returns>
        public static IObservable<long> Interval(TimeSpan period)
        {
            return Interval(period, UnitySchedulers.MainThread);
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after each period, using the specified scheduler.
        /// </summary>
        /// <param name="period">Period for producing the values in the resulting sequence.</param>
        /// <param name="scheduler">Scheduler to run timers on.</param>
        /// <returns>An observable sequence that produces a value after each period.</returns>
        public static IObservable<long> Interval(TimeSpan period, IScheduler scheduler)
        {
            if (period < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(period));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));

            return Create<long>(observer =>
            {
                long count = 0;
                return scheduler.SchedulePeriodic(count, period, c =>
                {
                    observer.OnNext(c);
                    return c + 1;
                });
            });
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after the due time has elapsed.
        /// </summary>
        /// <param name="dueTime">Relative time at which to produce the value.</param>
        /// <returns>An observable sequence that produces a value after the due time has elapsed.</returns>
        public static IObservable<long> Timer(TimeSpan dueTime)
        {
            return Timer(dueTime, UnitySchedulers.MainThread);
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after the due time has elapsed, using the specified scheduler.
        /// </summary>
        /// <param name="dueTime">Relative time at which to produce the value.</param>
        /// <param name="scheduler">Scheduler to run the timer on.</param>
        /// <returns>An observable sequence that produces a value after the due time has elapsed.</returns>
        public static IObservable<long> Timer(TimeSpan dueTime, IScheduler scheduler)
        {
            if (dueTime < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(dueTime));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));

            return Create<long>(observer =>
            {
                return scheduler.Schedule(0L, dueTime, (s, state) =>
                {
                    observer.OnNext(state);
                    observer.OnCompleted();
                    return Disposable.Empty;
                });
            });
        }

        /// <summary>
        /// Generates an observable sequence by running a state-driven loop producing the sequence's elements.
        /// </summary>
        /// <typeparam name="TState">The type of the state used in the generator loop.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the produced sequence.</typeparam>
        /// <param name="initialState">Initial state.</param>
        /// <param name="condition">Condition to terminate generation (upon returning false).</param>
        /// <param name="iterate">Iteration step function.</param>
        /// <param name="resultSelector">Selector function for results produced in the sequence.</param>
        /// <returns>The generated sequence.</returns>
        public static IObservable<TResult> Generate<TState, TResult>(
            TState initialState,
            Func<TState, bool> condition,
            Func<TState, TState> iterate,
            Func<TState, TResult> resultSelector)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (iterate == null)
                throw new ArgumentNullException(nameof(iterate));
            if (resultSelector == null)
                throw new ArgumentNullException(nameof(resultSelector));

            return Create<TResult>(observer =>
            {
                var state = initialState;
                var hasResult = true;

                try
                {
                    while (hasResult && condition(state))
                    {
                        var result = resultSelector(state);
                        observer.OnNext(result);
                        state = iterate(state);
                    }
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return Disposable.Empty;
            });
        }

        /// <summary>
        /// Converts an enumerable sequence to an observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Enumerable sequence to convert to an observable sequence.</param>
        /// <returns>The observable sequence whose elements are pulled from the given enumerable sequence.</returns>
        public static IObservable<T> ToObservable<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return Create<T>(observer =>
            {
                try
                {
                    foreach (var item in source)
                    {
                        observer.OnNext(item);
                    }
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return Disposable.Empty;
            });
        }

        /// <summary>
        /// Merges elements from all inner observable sequences into a single observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequences.</typeparam>
        /// <param name="sources">Observable sequences to merge.</param>
        /// <returns>The observable sequence that merges the elements of the inner sequences.</returns>
        public static IObservable<T> Merge<T>(params IObservable<T>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            return sources.ToObservable().Merge();
        }

        /// <summary>
        /// Merges elements from all observable sequences in the enumerable into a single observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequences.</typeparam>
        /// <param name="sources">Enumerable of observable sequences to merge.</param>
        /// <returns>The observable sequence that merges the elements of the inner sequences.</returns>
        public static IObservable<T> Merge<T>(this IEnumerable<IObservable<T>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            return sources.ToObservable().Merge();
        }

        /// <summary>
        /// Converts a coroutine to an observable sequence.
        /// </summary>
        /// <param name="coroutine">The coroutine to convert.</param>
        /// <returns>An observable sequence that completes when the coroutine finishes.</returns>
        public static IObservable<Unit> FromCoroutine(Func<IEnumerator> coroutine)
        {
            if (coroutine == null)
                throw new ArgumentNullException(nameof(coroutine));

            return Create<Unit>(observer =>
            {
                var runner = GetSchedulerRunner();
                var routine = runner.StartCoroutine(ExecuteCoroutine());
                
                return Disposable.Create(() =>
                {
                    if (routine != null && runner != null)
                        runner.StopCoroutine(routine);
                });

                IEnumerator ExecuteCoroutine()
                {
                    IEnumerator routine = null;
                    try
                    {
                        routine = coroutine();
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        yield break;
                    }

                    yield return routine;

                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                }
            });
        }

        /// <summary>
        /// Converts a coroutine that yields values to an observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the values yielded by the coroutine.</typeparam>
        /// <param name="coroutine">The coroutine to convert.</param>
        /// <returns>An observable sequence that emits the values yielded by the coroutine.</returns>
        public static IObservable<T> FromCoroutine<T>(Func<IObserver<T>, IEnumerator> coroutine)
        {
            if (coroutine == null)
                throw new ArgumentNullException(nameof(coroutine));

            return Create<T>(observer =>
            {
                var runner = GetSchedulerRunner();
                var routine = runner.StartCoroutine(coroutine(observer));

                return Disposable.Create(() =>
                {
                    if (routine != null && runner != null)
                        runner.StopCoroutine(routine);
                });
            });
        }

        /// <summary>
        /// Converts a UnityEvent to an observable sequence.
        /// </summary>
        /// <param name="unityEvent">The UnityEvent to convert.</param>
        /// <returns>An observable sequence that emits when the UnityEvent is invoked.</returns>
        public static IObservable<Unit> FromUnityEvent(UnityEngine.Events.UnityEvent unityEvent)
        {
            if (unityEvent == null)
                throw new ArgumentNullException(nameof(unityEvent));

            return Create<Unit>(observer =>
            {
                UnityEngine.Events.UnityAction handler = () => observer.OnNext(Unit.Default);
                unityEvent.AddListener(handler);
                return Disposable.Create(() => unityEvent.RemoveListener(handler));
            });
        }

        /// <summary>
        /// Converts a UnityEvent&lt;T&gt; to an observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the event argument.</typeparam>
        /// <param name="unityEvent">The UnityEvent to convert.</param>
        /// <returns>An observable sequence that emits the event arguments when the UnityEvent is invoked.</returns>
        public static IObservable<T> FromUnityEvent<T>(UnityEngine.Events.UnityEvent<T> unityEvent)
        {
            if (unityEvent == null)
                throw new ArgumentNullException(nameof(unityEvent));

            return Create<T>(observer =>
            {
                UnityEngine.Events.UnityAction<T> handler = value => observer.OnNext(value);
                unityEvent.AddListener(handler);
                return Disposable.Create(() => unityEvent.RemoveListener(handler));
            });
        }

        /// <summary>
        /// Converts an AsyncOperation to an observable sequence that tracks progress and completion.
        /// </summary>
        /// <param name="asyncOperation">The AsyncOperation to convert.</param>
        /// <returns>An observable sequence that emits progress values and completes when the operation finishes.</returns>
        public static IObservable<float> FromAsyncOperation(UnityEngine.AsyncOperation asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));

            return Create<float>(observer =>
            {
                if (asyncOperation.isDone)
                {
                    observer.OnNext(1.0f);
                    observer.OnCompleted();
                    return Disposable.Empty;
                }

                var runner = GetSchedulerRunner();
                var routine = runner.StartCoroutine(TrackAsyncOperation(asyncOperation, observer));

                return Disposable.Create(() =>
                {
                    if (routine != null && runner != null)
                        runner.StopCoroutine(routine);
                });
            });
        }

        /// <summary>
        /// Converts an AsyncOperation to an observable sequence that emits the operation itself when completed.
        /// </summary>
        /// <typeparam name="T">The type of AsyncOperation.</typeparam>
        /// <param name="asyncOperation">The AsyncOperation to convert.</param>
        /// <returns>An observable sequence that emits the AsyncOperation when it completes.</returns>
        public static IObservable<T> FromAsyncOperation<T>(T asyncOperation) where T : UnityEngine.AsyncOperation
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));

            return Create<T>(observer =>
            {
                if (asyncOperation.isDone)
                {
                    observer.OnNext(asyncOperation);
                    observer.OnCompleted();
                    return Disposable.Empty;
                }

                var runner = GetSchedulerRunner();
                var routine = runner.StartCoroutine(WaitForAsyncOperation(asyncOperation, observer));

                return Disposable.Create(() =>
                {
                    if (routine != null && runner != null)
                        runner.StopCoroutine(routine);
                });
            });
        }

        private static System.Collections.IEnumerator TrackAsyncOperation(UnityEngine.AsyncOperation operation, IObserver<float> observer)
        {
            while (!operation.isDone)
            {
                observer.OnNext(operation.progress);
                yield return null;
            }

            observer.OnNext(1.0f);
            observer.OnCompleted();
        }

        private static System.Collections.IEnumerator WaitForAsyncOperation<T>(T operation, IObserver<T> observer) where T : UnityEngine.AsyncOperation
        {
            yield return operation;
            observer.OnNext(operation);
            observer.OnCompleted();
        }

        /// <summary>
        /// Generates an observable sequence of integral numbers within a specified range.
        /// </summary>
        /// <param name="start">The value of the first integer in the sequence.</param>
        /// <param name="count">The number of sequential integers to generate.</param>
        /// <returns>An observable sequence that contains a range of sequential integral numbers.</returns>
        public static IObservable<int> Range(int start, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
                return Empty<int>();

            return Generate(start, x => x < start + count, x => x + 1, x => x);
        }

        /// <summary>
        /// Generates an observable sequence of integral numbers within a specified range, using the specified scheduler.
        /// </summary>
        /// <param name="start">The value of the first integer in the sequence.</param>
        /// <param name="count">The number of sequential integers to generate.</param>
        /// <param name="scheduler">Scheduler to run the generator loop on.</param>
        /// <returns>An observable sequence that contains a range of sequential integral numbers.</returns>
        public static IObservable<int> Range(int start, int count, IScheduler scheduler)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));

            if (count == 0)
                return Empty<int>();

            return Generate(start, x => x < start + count, x => x + 1, x => x)
                .SubscribeOn(scheduler);
        }

        private static MonoBehaviour GetSchedulerRunner()
        {
            // Access the scheduler runner directly
            return UnitySchedulerBase.Runner;
        }

        private sealed class AnonymousObservable<T> : IObservable<T>
        {
            private readonly Func<IObserver<T>, IDisposable> _subscribe;

            public AnonymousObservable(Func<IObserver<T>, IDisposable> subscribe)
            {
                _subscribe = subscribe;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                if (observer == null)
                    throw new ArgumentNullException(nameof(observer));

                try
                {
                    return _subscribe(observer);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                    return Disposable.Empty;
                }
            }
        }
    }
}
