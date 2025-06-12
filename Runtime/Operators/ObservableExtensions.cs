using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludo.Reactive
{
    /// <summary>
    /// Provides a set of extension methods for observable sequences.
    /// </summary>
    public static class ObservableExtensions
    {
        /// <summary>
        /// Subscribes to the observable sequence with the specified action.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Observable sequence to subscribe to.</param>
        /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));

            return source.Subscribe(Observer.Create(onNext));
        }

        /// <summary>
        /// Subscribes to the observable sequence with the specified actions.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Observable sequence to subscribe to.</param>
        /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
        /// <param name="onError">Action to invoke upon exceptional termination of the observable sequence.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            return source.Subscribe(Observer.Create(onNext, onError));
        }

        /// <summary>
        /// Subscribes to the observable sequence with the specified actions.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Observable sequence to subscribe to.</param>
        /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
        /// <param name="onCompleted">Action to invoke upon graceful termination of the observable sequence.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            return source.Subscribe(Observer.Create(onNext, onCompleted));
        }

        /// <summary>
        /// Subscribes to the observable sequence with the specified actions.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Observable sequence to subscribe to.</param>
        /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
        /// <param name="onError">Action to invoke upon exceptional termination of the observable sequence.</param>
        /// <param name="onCompleted">Action to invoke upon graceful termination of the observable sequence.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            return source.Subscribe(Observer.Create(onNext, onError, onCompleted));
        }

        /// <summary>
        /// Projects each element of an observable sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>An observable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        public static IObservable<TResult> Select<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return Observable.Create<TResult>(observer =>
            {
                return source.Subscribe(Observer.Create<TSource>(
                    value =>
                    {
                        try
                        {
                            var result = selector(value);
                            observer.OnNext(result);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                ));
            });
        }

        /// <summary>
        /// Projects each element of an observable sequence to an observable sequence and merges the resulting observable sequences into one observable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the projected inner sequences and the elements in the merged result sequence.</typeparam>
        /// <param name="source">An observable sequence of elements to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>An observable sequence whose elements are the result of invoking the one-to-many transform function on each element of the input sequence.</returns>
        public static IObservable<TResult> SelectMany<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, IObservable<TResult>> selector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return Observable.Create<TResult>(observer =>
            {
                var composite = new CompositeDisposable();
                var hasCompleted = false;
                var activeCount = 0;
                var lockObject = new object();

                composite.Add(source.Subscribe(Observer.Create<TSource>(
                    value =>
                    {
                        try
                        {
                            var inner = selector(value);
                            lock (lockObject)
                            {
                                activeCount++;
                            }

                            var innerSubscription = inner.Subscribe(Observer.Create<TResult>(
                                observer.OnNext,
                                observer.OnError,
                                () =>
                                {
                                    lock (lockObject)
                                    {
                                        activeCount--;
                                        if (hasCompleted && activeCount == 0)
                                        {
                                            observer.OnCompleted();
                                        }
                                    }
                                }
                            ));

                            composite.Add(innerSubscription);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        lock (lockObject)
                        {
                            hasCompleted = true;
                            if (activeCount == 0)
                            {
                                observer.OnCompleted();
                            }
                        }
                    }
                )));

                return composite;
            });
        }

        /// <summary>
        /// Filters the elements of an observable sequence based on a predicate.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An observable sequence whose elements to filter.</param>
        /// <param name="predicate">A function to test each source element for a condition.</param>
        /// <returns>An observable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        public static IObservable<T> Where<T>(
            this IObservable<T> source,
            Func<T, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return Observable.Create<T>(observer =>
            {
                return source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        try
                        {
                            if (predicate(value))
                            {
                                observer.OnNext(value);
                            }
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                ));
            });
        }

        /// <summary>
        /// Returns an observable sequence that contains only distinct contiguous elements according to the default equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An observable sequence to retain distinct contiguous elements for.</param>
        /// <returns>An observable sequence only containing the distinct contiguous elements from the source sequence.</returns>
        public static IObservable<T> DistinctUntilChanged<T>(this IObservable<T> source)
        {
            return DistinctUntilChanged(source, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Returns an observable sequence that contains only distinct contiguous elements according to the specified equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An observable sequence to retain distinct contiguous elements for.</param>
        /// <param name="comparer">Equality comparer for source elements.</param>
        /// <returns>An observable sequence only containing the distinct contiguous elements from the source sequence.</returns>
        public static IObservable<T> DistinctUntilChanged<T>(
            this IObservable<T> source,
            IEqualityComparer<T> comparer)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            return Observable.Create<T>(observer =>
            {
                var hasValue = false;
                var lastValue = default(T);

                return source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        var shouldEmit = false;

                        if (!hasValue)
                        {
                            hasValue = true;
                            shouldEmit = true;
                        }
                        else
                        {
                            try
                            {
                                shouldEmit = !comparer.Equals(value, lastValue);
                            }
                            catch (Exception ex)
                            {
                                observer.OnError(ex);
                                return;
                            }
                        }

                        if (shouldEmit)
                        {
                            lastValue = value;
                            observer.OnNext(value);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                ));
            });
        }

        /// <summary>
        /// Returns the elements from the source observable sequence only after the specified duration has passed without another value being emitted.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to throttle.</param>
        /// <param name="dueTime">Throttling duration for each element.</param>
        /// <returns>The throttled observable sequence.</returns>
        public static IObservable<T> Throttle<T>(this IObservable<T> source, TimeSpan dueTime)
        {
            return Throttle(source, dueTime, UnitySchedulers.MainThread);
        }

        /// <summary>
        /// Returns the elements from the source observable sequence only after the specified duration has passed without another value being emitted.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to throttle.</param>
        /// <param name="dueTime">Throttling duration for each element.</param>
        /// <param name="scheduler">Scheduler to run the throttle timers on.</param>
        /// <returns>The throttled observable sequence.</returns>
        public static IObservable<T> Throttle<T>(
            this IObservable<T> source,
            TimeSpan dueTime,
            IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (dueTime < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(dueTime));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));

            return Observable.Create<T>(observer =>
            {
                var composite = new CompositeDisposable();
                var hasValue = false;
                var value = default(T);
                var id = 0UL;
                var lockObject = new object();

                composite.Add(source.Subscribe(Observer.Create<T>(
                    newValue =>
                    {
                        ulong currentId;
                        lock (lockObject)
                        {
                            hasValue = true;
                            value = newValue;
                            currentId = ++id;
                        }

                        var timer = scheduler.Schedule(currentId, dueTime, (s, scheduledId) =>
                        {
                            lock (lockObject)
                            {
                                if (hasValue && id == scheduledId)
                                {
                                    observer.OnNext(value);
                                    hasValue = false;
                                }
                            }
                            return Disposable.Empty;
                        });

                        composite.Add(timer);
                    },
                    observer.OnError,
                    () =>
                    {
                        lock (lockObject)
                        {
                            if (hasValue)
                            {
                                observer.OnNext(value);
                            }
                        }
                        observer.OnCompleted();
                    }
                )));

                return composite;
            });
        }

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of an observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The sequence to take elements from.</param>
        /// <param name="count">The number of elements to take from the start of the source sequence.</param>
        /// <returns>An observable sequence that contains the specified number of elements from the start of the input sequence.</returns>
        public static IObservable<T> Take<T>(this IObservable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
                return Observable.Empty<T>();

            return Observable.Create<T>(observer =>
            {
                var remaining = count;

                return source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        if (remaining > 0)
                        {
                            remaining--;
                            observer.OnNext(value);

                            if (remaining == 0)
                            {
                                observer.OnCompleted();
                            }
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                ));
            });
        }

        /// <summary>
        /// Invokes an action for each element in the observable sequence and invokes an action upon graceful or exceptional termination of the observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
        /// <returns>The source sequence with the side-effecting behavior applied.</returns>
        public static IObservable<T> Do<T>(this IObservable<T> source, Action<T> onNext)
        {
            return Do(source, onNext, ex => { }, () => { });
        }

        /// <summary>
        /// Invokes an action for each element in the observable sequence and invokes an action upon graceful or exceptional termination of the observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
        /// <param name="onError">Action to invoke upon exceptional termination of the observable sequence.</param>
        /// <param name="onCompleted">Action to invoke upon graceful termination of the observable sequence.</param>
        /// <returns>The source sequence with the side-effecting behavior applied.</returns>
        public static IObservable<T> Do<T>(
            this IObservable<T> source,
            Action<T> onNext,
            Action<Exception> onError,
            Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            return Observable.Create<T>(observer =>
            {
                return source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        try
                        {
                            onNext(value);
                            observer.OnNext(value);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    error =>
                    {
                        try
                        {
                            onError(error);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            return;
                        }
                        observer.OnError(error);
                    },
                    () =>
                    {
                        try
                        {
                            onCompleted();
                            observer.OnCompleted();
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    }
                ));
            });
        }

        /// <summary>
        /// Merges the specified observable sequences into one observable sequence by using the selector function whenever all of the observable sequences have produced an element at a corresponding index.
        /// </summary>
        /// <typeparam name="TFirst">The type of the elements in the first source sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements in the second source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence.</typeparam>
        /// <param name="first">First observable source.</param>
        /// <param name="second">Second observable source.</param>
        /// <param name="resultSelector">Function to invoke whenever either of the sources produces an element.</param>
        /// <returns>An observable sequence containing the result of combining elements of the sources using the specified result selector function.</returns>
        public static IObservable<TResult> CombineLatest<TFirst, TSecond, TResult>(
            this IObservable<TFirst> first,
            IObservable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));
            if (resultSelector == null)
                throw new ArgumentNullException(nameof(resultSelector));

            return Observable.Create<TResult>(observer =>
            {
                var composite = new CompositeDisposable();
                var lockObject = new object();
                var hasFirst = false;
                var hasSecond = false;
                var firstValue = default(TFirst);
                var secondValue = default(TSecond);
                var firstCompleted = false;
                var secondCompleted = false;

                composite.Add(first.Subscribe(Observer.Create<TFirst>(
                    value =>
                    {
                        lock (lockObject)
                        {
                            hasFirst = true;
                            firstValue = value;

                            if (hasSecond)
                            {
                                try
                                {
                                    var result = resultSelector(firstValue, secondValue);
                                    observer.OnNext(result);
                                }
                                catch (Exception ex)
                                {
                                    observer.OnError(ex);
                                }
                            }
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        lock (lockObject)
                        {
                            firstCompleted = true;
                            if (secondCompleted)
                            {
                                observer.OnCompleted();
                            }
                        }
                    }
                )));

                composite.Add(second.Subscribe(Observer.Create<TSecond>(
                    value =>
                    {
                        lock (lockObject)
                        {
                            hasSecond = true;
                            secondValue = value;

                            if (hasFirst)
                            {
                                try
                                {
                                    var result = resultSelector(firstValue, secondValue);
                                    observer.OnNext(result);
                                }
                                catch (Exception ex)
                                {
                                    observer.OnError(ex);
                                }
                            }
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        lock (lockObject)
                        {
                            secondCompleted = true;
                            if (firstCompleted)
                            {
                                observer.OnCompleted();
                            }
                        }
                    }
                )));

                return composite;
            });
        }

        /// <summary>
        /// Flattens the observable sequences from the source observable into a single observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequences.</typeparam>
        /// <param name="sources">Observable sequence of inner observable sequences.</param>
        /// <returns>An observable sequence whose elements are the result of flattening the inner observable sequences.</returns>
        public static IObservable<T> Merge<T>(this IObservable<IObservable<T>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            return sources.SelectMany(x => x);
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
        /// Time shifts the observable sequence by delaying the subscription with the specified relative time duration.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to delay subscription for.</param>
        /// <param name="dueTime">Relative time by which to delay the subscription.</param>
        /// <returns>Time-shifted sequence.</returns>
        public static IObservable<T> Delay<T>(this IObservable<T> source, TimeSpan dueTime)
        {
            return Delay(source, dueTime, UnitySchedulers.MainThread);
        }

        /// <summary>
        /// Time shifts the observable sequence by delaying the subscription with the specified relative time duration, using the specified scheduler to run timers.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to delay subscription for.</param>
        /// <param name="dueTime">Relative time by which to delay the subscription.</param>
        /// <param name="scheduler">Scheduler to run the delay timers on.</param>
        /// <returns>Time-shifted sequence.</returns>
        public static IObservable<T> Delay<T>(
            this IObservable<T> source,
            TimeSpan dueTime,
            IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (dueTime < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(dueTime));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));

            return Observable.Create<T>(observer =>
            {
                return scheduler.Schedule(Unit.Default, dueTime, (s, _) =>
                {
                    return source.Subscribe(observer);
                });
            });
        }

        /// <summary>
        /// Applies a timeout policy for each element in the observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to perform a timeout for.</param>
        /// <param name="dueTime">Maximum duration between values before a timeout occurs.</param>
        /// <returns>The source sequence with a timeout policy applied.</returns>
        public static IObservable<T> Timeout<T>(this IObservable<T> source, TimeSpan dueTime)
        {
            return Timeout(source, dueTime, UnitySchedulers.MainThread);
        }

        /// <summary>
        /// Applies a timeout policy for each element in the observable sequence, using the specified scheduler to run timeout timers.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to perform a timeout for.</param>
        /// <param name="dueTime">Maximum duration between values before a timeout occurs.</param>
        /// <param name="scheduler">Scheduler to run the timeout timers on.</param>
        /// <returns>The source sequence with a timeout policy applied.</returns>
        public static IObservable<T> Timeout<T>(
            this IObservable<T> source,
            TimeSpan dueTime,
            IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (dueTime < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(dueTime));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));

            return Observable.Create<T>(observer =>
            {
                var composite = new CompositeDisposable();
                var lockObject = new object();
                var id = 0UL;

                composite.Add(source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        ulong currentId;
                        lock (lockObject)
                        {
                            currentId = ++id;
                        }

                        observer.OnNext(value);

                        var timer = scheduler.Schedule(currentId, dueTime, (s, scheduledId) =>
                        {
                            lock (lockObject)
                            {
                                if (id == scheduledId)
                                {
                                    observer.OnError(new TimeoutException("The operation has timed out."));
                                }
                            }
                            return Disposable.Empty;
                        });

                        composite.Add(timer);
                    },
                    observer.OnError,
                    observer.OnCompleted
                )));

                return composite;
            });
        }

        /// <summary>
        /// Projects each element of an observable sequence into zero or more buffers which are produced based on element count information.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to produce buffers over.</param>
        /// <param name="count">Length of each buffer.</param>
        /// <returns>An observable sequence of buffers.</returns>
        public static IObservable<IList<T>> Buffer<T>(this IObservable<T> source, int count)
        {
            return Buffer(source, count, count);
        }

        /// <summary>
        /// Projects each element of an observable sequence into zero or more buffers which are produced based on element count information.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to produce buffers over.</param>
        /// <param name="count">Length of each buffer.</param>
        /// <param name="skip">Number of elements to skip between creation of consecutive buffers.</param>
        /// <returns>An observable sequence of buffers.</returns>
        public static IObservable<IList<T>> Buffer<T>(this IObservable<T> source, int count, int skip)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (skip <= 0)
                throw new ArgumentOutOfRangeException(nameof(skip));

            return Observable.Create<IList<T>>(observer =>
            {
                var buffers = new List<List<T>>();
                var index = 0;

                return source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        // Add to existing buffers
                        for (int i = buffers.Count - 1; i >= 0; i--)
                        {
                            buffers[i].Add(value);
                            if (buffers[i].Count == count)
                            {
                                observer.OnNext(buffers[i]);
                                buffers.RemoveAt(i);
                            }
                        }

                        // Start new buffer if needed
                        if (index % skip == 0)
                        {
                            var newBuffer = new List<T> { value };
                            if (count == 1)
                            {
                                observer.OnNext(newBuffer);
                            }
                            else
                            {
                                buffers.Add(newBuffer);
                            }
                        }

                        index++;
                    },
                    observer.OnError,
                    () =>
                    {
                        // Emit remaining buffers
                        foreach (var buffer in buffers)
                        {
                            if (buffer.Count > 0)
                            {
                                observer.OnNext(buffer);
                            }
                        }
                        observer.OnCompleted();
                    }
                ));
            });
        }

        /// <summary>
        /// Projects each element of an observable sequence into zero or more buffers which are produced based on time information.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to produce buffers over.</param>
        /// <param name="timeSpan">Maximum time length of a buffer.</param>
        /// <returns>An observable sequence of buffers.</returns>
        public static IObservable<IList<T>> Buffer<T>(this IObservable<T> source, TimeSpan timeSpan)
        {
            return Buffer(source, timeSpan, UnitySchedulers.MainThread);
        }

        /// <summary>
        /// Projects each element of an observable sequence into zero or more buffers which are produced based on time information.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to produce buffers over.</param>
        /// <param name="timeSpan">Maximum time length of a buffer.</param>
        /// <param name="scheduler">Scheduler to run timers on.</param>
        /// <returns>An observable sequence of buffers.</returns>
        public static IObservable<IList<T>> Buffer<T>(this IObservable<T> source, TimeSpan timeSpan, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (timeSpan < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeSpan));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));

            return Observable.Create<IList<T>>(observer =>
            {
                var composite = new CompositeDisposable();
                var buffer = new List<T>();
                var lockObject = new object();

                var timer = scheduler.Schedule(Unit.Default, timeSpan, (s, _) =>
                {
                    List<T> currentBuffer;
                    lock (lockObject)
                    {
                        currentBuffer = new List<T>(buffer);
                        buffer.Clear();
                    }

                    if (currentBuffer.Count > 0)
                    {
                        observer.OnNext(currentBuffer);
                    }

                    return s.Schedule(Unit.Default, timeSpan, (s2, _2) => s2.Schedule(Unit.Default, timeSpan, (s3, _3) => Disposable.Empty));
                });

                composite.Add(timer);

                composite.Add(source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        lock (lockObject)
                        {
                            buffer.Add(value);
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        lock (lockObject)
                        {
                            if (buffer.Count > 0)
                            {
                                observer.OnNext(buffer);
                            }
                        }
                        observer.OnCompleted();
                    }
                )));

                return composite;
            });
        }

        /// <summary>
        /// Applies an accumulator function over an observable sequence and returns each intermediate result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">An observable sequence to accumulate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An observable sequence containing the accumulated values.</returns>
        public static IObservable<TAccumulate> Scan<TSource, TAccumulate>(
            this IObservable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (accumulator == null)
                throw new ArgumentNullException(nameof(accumulator));

            return Observable.Create<TAccumulate>(observer =>
            {
                var acc = seed;
                var hasValue = false;

                return source.Subscribe(Observer.Create<TSource>(
                    value =>
                    {
                        try
                        {
                            acc = accumulator(acc, value);
                            hasValue = true;
                            observer.OnNext(acc);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                ));
            });
        }

        /// <summary>
        /// Applies an accumulator function over an observable sequence and returns each intermediate result.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence and the result of the accumulation.</typeparam>
        /// <param name="source">An observable sequence to accumulate over.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An observable sequence containing the accumulated values.</returns>
        public static IObservable<T> Scan<T>(
            this IObservable<T> source,
            Func<T, T, T> accumulator)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (accumulator == null)
                throw new ArgumentNullException(nameof(accumulator));

            return Observable.Create<T>(observer =>
            {
                var acc = default(T);
                var hasValue = false;

                return source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        try
                        {
                            if (hasValue)
                            {
                                acc = accumulator(acc, value);
                            }
                            else
                            {
                                acc = value;
                                hasValue = true;
                            }
                            observer.OnNext(acc);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                ));
            });
        }

        /// <summary>
        /// Applies an accumulator function over an observable sequence and returns the final accumulated value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">An observable sequence to accumulate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <returns>An observable sequence containing a single element with the final accumulated value.</returns>
        public static IObservable<TResult> Aggregate<TSource, TAccumulate, TResult>(
            this IObservable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            Func<TAccumulate, TResult> resultSelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (accumulator == null)
                throw new ArgumentNullException(nameof(accumulator));
            if (resultSelector == null)
                throw new ArgumentNullException(nameof(resultSelector));

            return Observable.Create<TResult>(observer =>
            {
                var acc = seed;

                return source.Subscribe(Observer.Create<TSource>(
                    value =>
                    {
                        try
                        {
                            acc = accumulator(acc, value);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        try
                        {
                            var result = resultSelector(acc);
                            observer.OnNext(result);
                            observer.OnCompleted();
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    }
                ));
            });
        }

        /// <summary>
        /// Applies an accumulator function over an observable sequence and returns the final accumulated value.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence and the result of the accumulation.</typeparam>
        /// <param name="source">An observable sequence to accumulate over.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An observable sequence containing a single element with the final accumulated value.</returns>
        public static IObservable<T> Aggregate<T>(
            this IObservable<T> source,
            Func<T, T, T> accumulator)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (accumulator == null)
                throw new ArgumentNullException(nameof(accumulator));

            return Observable.Create<T>(observer =>
            {
                var acc = default(T);
                var hasValue = false;

                return source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        try
                        {
                            if (hasValue)
                            {
                                acc = accumulator(acc, value);
                            }
                            else
                            {
                                acc = value;
                                hasValue = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        if (hasValue)
                        {
                            observer.OnNext(acc);
                            observer.OnCompleted();
                        }
                        else
                        {
                            observer.OnError(new InvalidOperationException("Sequence contains no elements"));
                        }
                    }
                ));
            });
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the observable sequence produced by the handler.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence and the sequences produced by the exception handler function.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="handler">Exception handler function, producing another observable sequence.</param>
        /// <returns>An observable sequence containing the source sequence's elements, followed by the elements produced by the handler's resulting observable sequence in case an exception occurred.</returns>
        public static IObservable<T> Catch<T>(
            this IObservable<T> source,
            Func<Exception, IObservable<T>> handler)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return Observable.Create<T>(observer =>
            {
                var composite = new CompositeDisposable();

                composite.Add(source.Subscribe(Observer.Create<T>(
                    observer.OnNext,
                    ex =>
                    {
                        try
                        {
                            var recovery = handler(ex);
                            var recoverySubscription = recovery.Subscribe(observer);
                            composite.Add(recoverySubscription);
                        }
                        catch (Exception handlerEx)
                        {
                            observer.OnError(handlerEx);
                        }
                    },
                    observer.OnCompleted
                )));

                return composite;
            });
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception of the specified type with the observable sequence produced by the handler.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence and the sequences produced by the exception handler function.</typeparam>
        /// <typeparam name="TException">The type of the exception to catch and handle.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="handler">Exception handler function, producing another observable sequence.</param>
        /// <returns>An observable sequence containing the source sequence's elements, followed by the elements produced by the handler's resulting observable sequence in case an exception occurred.</returns>
        public static IObservable<T> Catch<T, TException>(
            this IObservable<T> source,
            Func<TException, IObservable<T>> handler)
            where TException : Exception
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return source.Catch(ex =>
            {
                if (ex is TException typedException)
                {
                    return handler(typedException);
                }
                return Observable.Throw<T>(ex);
            });
        }

        /// <summary>
        /// Logs errors from the observable sequence using Unity's Debug.LogError and continues with the sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="context">Optional context string to include in the log message.</param>
        /// <returns>The source sequence with error logging applied.</returns>
        public static IObservable<T> LogError<T>(
            this IObservable<T> source,
            string context = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Do(
                onNext: _ => { },
                onError: ex =>
                {
                    var message = string.IsNullOrEmpty(context)
                        ? $"[Ludo.Reactive] Observable error: {ex}"
                        : $"[Ludo.Reactive] {context}: {ex}";
                    UnityEngine.Debug.LogError(message);
                },
                onCompleted: () => { }
            );
        }



        /// <summary>
        /// Wraps the source sequence in order to run its observer callbacks on the specified scheduler.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="scheduler">Scheduler to notify observers on.</param>
        /// <returns>The source sequence whose observations happen on the specified scheduler.</returns>
        public static IObservable<T> ObserveOn<T>(this IObservable<T> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));

            return Observable.Create<T>(observer =>
            {
                return source.Subscribe(Observer.Create<T>(
                    value => scheduler.Schedule(Unit.Default, (s, _) =>
                    {
                        observer.OnNext(value);
                        return Disposable.Empty;
                    }),
                    ex => scheduler.Schedule(Unit.Default, (s, _) =>
                    {
                        observer.OnError(ex);
                        return Disposable.Empty;
                    }),
                    () => scheduler.Schedule(Unit.Default, (s, _) =>
                    {
                        observer.OnCompleted();
                        return Disposable.Empty;
                    })
                ));
            });
        }

        /// <summary>
        /// Wraps the source sequence in order to run its subscription and unsubscription logic on the specified scheduler.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="scheduler">Scheduler to perform subscription and unsubscription actions on.</param>
        /// <returns>The source sequence whose subscriptions and unsubscriptions happen on the specified scheduler.</returns>
        public static IObservable<T> SubscribeOn<T>(this IObservable<T> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));

            return Observable.Create<T>(observer =>
            {
                var composite = new CompositeDisposable();
                var subscription = scheduler.Schedule(Unit.Default, (s, _) =>
                {
                    var innerSubscription = source.Subscribe(observer);
                    composite.Add(innerSubscription);
                    return Disposable.Empty;
                });

                composite.Add(subscription);
                return composite;
            });
        }

        /// <summary>
        /// Converts the elements of an observable sequence to the specified type.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type to convert the elements to.</typeparam>
        /// <param name="source">The observable sequence to convert.</param>
        /// <returns>An observable sequence that contains each element of the source sequence converted to the specified type.</returns>
        public static IObservable<TResult> Cast<TSource, TResult>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Select(x => (TResult)(object)x);
        }

        /// <summary>
        /// Filters the elements of an observable sequence based on a specified type.
        /// </summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">The observable sequence to filter.</param>
        /// <returns>An observable sequence that contains elements from the input sequence of type TResult.</returns>
        public static IObservable<TResult> OfType<TResult>(this IObservable<object> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Where(x => x is TResult).Cast<object, TResult>();
        }

        /// <summary>
        /// Filters the elements of an observable sequence based on a specified type.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">The observable sequence to filter.</param>
        /// <returns>An observable sequence that contains elements from the input sequence of type TResult.</returns>
        public static IObservable<TResult> OfType<TSource, TResult>(this IObservable<TSource> source)
            where TResult : class
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return Observable.Create<TResult>(observer =>
            {
                return source.Subscribe(Observer.Create<TSource>(
                    value =>
                    {
                        if (value is TResult result)
                        {
                            observer.OnNext(result);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                ));
            });
        }

        /// <summary>
        /// Converts an observable sequence to a Unit observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The observable sequence to convert.</param>
        /// <returns>An observable sequence that emits Unit for each element in the source sequence.</returns>
        public static IObservable<Unit> AsUnitObservable<T>(this IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Select(_ => Unit.Default);
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence whose elements will be multicasted through a single shared subscription.</param>
        /// <returns>A connectable observable sequence that shares a single subscription to the underlying sequence.</returns>
        public static IConnectableObservable<T> Publish<T>(this IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new ConnectableObservable<T>(source, () => new Subject<T>());
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying sequence replaying all notifications.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence whose elements will be multicasted through a single shared subscription.</param>
        /// <returns>A connectable observable sequence that shares a single subscription to the underlying sequence.</returns>
        public static IConnectableObservable<T> Replay<T>(this IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Use a large but reasonable buffer size (1 million elements)
            return new ConnectableObservable<T>(source, () => new ReplaySubject<T>(1000000));
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying sequence replaying bufferSize notifications.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence whose elements will be multicasted through a single shared subscription.</param>
        /// <param name="bufferSize">Maximum element count of the replay buffer.</param>
        /// <returns>A connectable observable sequence that shares a single subscription to the underlying sequence.</returns>
        public static IConnectableObservable<T> Replay<T>(this IObservable<T> source, int bufferSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new ConnectableObservable<T>(source, () => new ReplaySubject<T>(bufferSize));
        }

        /// <summary>
        /// Returns an observable sequence that stays connected to the source as long as there is at least one subscription to the observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Connectable observable sequence.</param>
        /// <returns>An observable sequence that stays connected to the source as long as there is at least one subscription to the observable sequence.</returns>
        public static IObservable<T> RefCount<T>(this IConnectableObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return Observable.Create<T>(observer =>
            {
                var subscription = source.Subscribe(observer);
                var connection = source.Connect();

                return Disposable.Create(() =>
                {
                    subscription?.Dispose();
                    connection?.Dispose();
                });
            });
        }



        /// <summary>
        /// Merges three observable sequences into one observable sequence by using the selector function whenever one of the observable sequences produces an element.
        /// </summary>
        /// <typeparam name="T1">The type of the elements in the first source sequence.</typeparam>
        /// <typeparam name="T2">The type of the elements in the second source sequence.</typeparam>
        /// <typeparam name="T3">The type of the elements in the third source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence.</typeparam>
        /// <param name="source1">First observable source.</param>
        /// <param name="source2">Second observable source.</param>
        /// <param name="source3">Third observable source.</param>
        /// <param name="resultSelector">Function to invoke whenever any of the sources produces an element.</param>
        /// <returns>An observable sequence containing the result of combining elements of all sources using the specified result selector function.</returns>
        public static IObservable<TResult> CombineLatest<T1, T2, T3, TResult>(
            this IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
            Func<T1, T2, T3, TResult> resultSelector)
        {
            if (source1 == null)
                throw new ArgumentNullException(nameof(source1));
            if (source2 == null)
                throw new ArgumentNullException(nameof(source2));
            if (source3 == null)
                throw new ArgumentNullException(nameof(source3));
            if (resultSelector == null)
                throw new ArgumentNullException(nameof(resultSelector));

            return source1.CombineLatest(source2, (v1, v2) => new { v1, v2 })
                          .CombineLatest(source3, (combined, v3) => resultSelector(combined.v1, combined.v2, v3));
        }
    }
}
