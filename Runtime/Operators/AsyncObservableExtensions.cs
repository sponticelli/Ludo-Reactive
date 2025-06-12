using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// Provides extension methods for integrating observable sequences with async/await patterns.
    /// </summary>
    public static class AsyncObservableExtensions
    {
        /// <summary>
        /// Converts an observable sequence to a Task that completes when the first element is received.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The observable sequence to convert.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A Task that completes with the first element from the observable sequence.</returns>
        public static Task<T> ToTask<T>(this IObservable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var tcs = new TaskCompletionSource<T>();
            var subscription = default(IDisposable);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    subscription?.Dispose();
                    tcs.TrySetCanceled();
                });
            }

            subscription = source.Subscribe(Observer.Create<T>(
                value =>
                {
                    subscription?.Dispose();
                    tcs.TrySetResult(value);
                },
                ex =>
                {
                    subscription?.Dispose();
                    tcs.TrySetException(ex);
                },
                () =>
                {
                    subscription?.Dispose();
                    tcs.TrySetException(new InvalidOperationException("Sequence completed without producing any elements"));
                }
            ));

            return tcs.Task;
        }

        /// <summary>
        /// Converts an observable sequence to a Task that completes when the sequence completes, returning all elements as an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The observable sequence to convert.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A Task that completes with all elements from the observable sequence as an array.</returns>
        public static Task<T[]> ToArrayAsync<T>(this IObservable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var tcs = new TaskCompletionSource<T[]>();
            var list = new List<T>();
            var subscription = default(IDisposable);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    subscription?.Dispose();
                    tcs.TrySetCanceled();
                });
            }

            subscription = source.Subscribe(Observer.Create<T>(
                value => list.Add(value),
                ex =>
                {
                    subscription?.Dispose();
                    tcs.TrySetException(ex);
                },
                () =>
                {
                    subscription?.Dispose();
                    tcs.TrySetResult(list.ToArray());
                }
            ));

            return tcs.Task;
        }

        /// <summary>
        /// Converts a Task to an observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the result produced by the task.</typeparam>
        /// <param name="task">The task to convert.</param>
        /// <returns>An observable sequence that produces the task's result when it completes.</returns>
        public static IObservable<T> ToObservable<T>(this Task<T> task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            return Observable.Create<T>(observer =>
            {
                if (task.IsCompleted)
                {
                    if (task.IsFaulted)
                    {
                        observer.OnError(task.Exception?.GetBaseException() ?? new Exception("Task faulted"));
                    }
                    else if (task.IsCanceled)
                    {
                        observer.OnError(new OperationCanceledException());
                    }
                    else
                    {
                        observer.OnNext(task.Result);
                        observer.OnCompleted();
                    }
                    return Disposable.Empty;
                }

                var cts = new CancellationTokenSource();
                
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        observer.OnError(t.Exception?.GetBaseException() ?? new Exception("Task faulted"));
                    }
                    else if (t.IsCanceled)
                    {
                        observer.OnError(new OperationCanceledException());
                    }
                    else
                    {
                        observer.OnNext(t.Result);
                        observer.OnCompleted();
                    }
                }, cts.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                return Disposable.Create(() => cts.Cancel());
            });
        }

        /// <summary>
        /// Converts a Task to an observable sequence that emits Unit when the task completes.
        /// </summary>
        /// <param name="task">The task to convert.</param>
        /// <returns>An observable sequence that emits Unit when the task completes.</returns>
        public static IObservable<Unit> ToObservable(this Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            return Observable.Create<Unit>(observer =>
            {
                if (task.IsCompleted)
                {
                    if (task.IsFaulted)
                    {
                        observer.OnError(task.Exception?.GetBaseException() ?? new Exception("Task faulted"));
                    }
                    else if (task.IsCanceled)
                    {
                        observer.OnError(new OperationCanceledException());
                    }
                    else
                    {
                        observer.OnNext(Unit.Default);
                        observer.OnCompleted();
                    }
                    return Disposable.Empty;
                }

                var cts = new CancellationTokenSource();
                
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        observer.OnError(t.Exception?.GetBaseException() ?? new Exception("Task faulted"));
                    }
                    else if (t.IsCanceled)
                    {
                        observer.OnError(new OperationCanceledException());
                    }
                    else
                    {
                        observer.OnNext(Unit.Default);
                        observer.OnCompleted();
                    }
                }, cts.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                return Disposable.Create(() => cts.Cancel());
            });
        }

        /// <summary>
        /// Converts an AsyncOperation to an observable sequence that tracks progress.
        /// </summary>
        /// <param name="asyncOperation">The AsyncOperation to convert.</param>
        /// <returns>An observable sequence that emits progress values.</returns>
        public static IObservable<float> ToObservableWithProgress(this AsyncOperation asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));

            return Observable.FromAsyncOperation(asyncOperation);
        }

        /// <summary>
        /// Converts an AsyncOperation to an observable sequence that emits only when the operation completes.
        /// </summary>
        /// <typeparam name="T">The type of AsyncOperation.</typeparam>
        /// <param name="asyncOperation">The AsyncOperation to convert.</param>
        /// <returns>An observable sequence that emits the AsyncOperation when it completes.</returns>
        public static IObservable<T> ToObservable<T>(this T asyncOperation) where T : AsyncOperation
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));

            return Observable.FromAsyncOperation(asyncOperation);
        }

        /// <summary>
        /// Observes the observable sequence on Unity's main thread using async/await patterns.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The observable sequence to observe.</param>
        /// <returns>An observable sequence that emits on Unity's main thread.</returns>
        public static IObservable<T> ObserveOnMainThread<T>(this IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.ObserveOn(UnitySchedulers.MainThread);
        }

        /// <summary>
        /// Subscribes to the observable sequence on Unity's main thread using async/await patterns.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The observable sequence to subscribe to.</param>
        /// <returns>An observable sequence that subscribes on Unity's main thread.</returns>
        public static IObservable<T> SubscribeOnMainThread<T>(this IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.SubscribeOn(UnitySchedulers.MainThread);
        }
    }
}
