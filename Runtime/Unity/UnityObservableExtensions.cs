using System;
using System.Collections;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// Provides Unity-specific extension methods for observable sequences.
    /// </summary>
    public static class UnityObservableExtensions
    {
        /// <summary>
        /// Automatically disposes the subscription when the specified Component is destroyed.
        /// </summary>
        /// <typeparam name="T">The type of the disposable.</typeparam>
        /// <param name="disposable">The disposable to manage.</param>
        /// <param name="component">The component whose lifetime will control the disposal.</param>
        /// <returns>The original disposable.</returns>
        public static T AddTo<T>(this T disposable, Component component) where T : IDisposable
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            var tracker = component.GetComponent<DisposableTracker>();
            if (tracker == null)
            {
                tracker = component.gameObject.AddComponent<DisposableTracker>();
            }

            tracker.Add(disposable);
            return disposable;
        }

        /// <summary>
        /// Automatically disposes the subscription when the specified GameObject is destroyed.
        /// </summary>
        /// <typeparam name="T">The type of the disposable.</typeparam>
        /// <param name="disposable">The disposable to manage.</param>
        /// <param name="gameObject">The GameObject whose lifetime will control the disposal.</param>
        /// <returns>The original disposable.</returns>
        public static T AddTo<T>(this T disposable, GameObject gameObject) where T : IDisposable
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            var tracker = gameObject.GetComponent<DisposableTracker>();
            if (tracker == null)
            {
                tracker = gameObject.AddComponent<DisposableTracker>();
            }

            tracker.Add(disposable);
            return disposable;
        }

        /// <summary>
        /// Observes the OnDestroy event of the specified Component.
        /// </summary>
        /// <param name="component">The component to observe.</param>
        /// <returns>An observable that emits when the component is destroyed.</returns>
        public static IObservable<Unit> OnDestroyAsObservable(this Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            return Observable.Create<Unit>(observer =>
            {
                var trigger = component.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = component.gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.OnDestroyAsObservable().Subscribe(observer);
            });
        }

        /// <summary>
        /// Observes the OnEnable event of the specified Component.
        /// </summary>
        /// <param name="component">The component to observe.</param>
        /// <returns>An observable that emits when the component is enabled.</returns>
        public static IObservable<Unit> OnEnableAsObservable(this Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            return Observable.Create<Unit>(observer =>
            {
                var trigger = component.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = component.gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.OnEnableAsObservable().Subscribe(observer);
            });
        }

        /// <summary>
        /// Observes the OnDisable event of the specified Component.
        /// </summary>
        /// <param name="component">The component to observe.</param>
        /// <returns>An observable that emits when the component is disabled.</returns>
        public static IObservable<Unit> OnDisableAsObservable(this Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            return Observable.Create<Unit>(observer =>
            {
                var trigger = component.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = component.gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.OnDisableAsObservable().Subscribe(observer);
            });
        }

        /// <summary>
        /// Observes Unity's Update loop.
        /// </summary>
        /// <param name="component">The component to observe.</param>
        /// <returns>An observable that emits every frame during Update.</returns>
        public static IObservable<Unit> UpdateAsObservable(this Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            return Observable.Create<Unit>(observer =>
            {
                var trigger = component.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = component.gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.UpdateAsObservable().Subscribe(observer);
            });
        }

        /// <summary>
        /// Observes Unity's FixedUpdate loop.
        /// </summary>
        /// <param name="component">The component to observe.</param>
        /// <returns>An observable that emits every fixed frame during FixedUpdate.</returns>
        public static IObservable<Unit> FixedUpdateAsObservable(this Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            return Observable.Create<Unit>(observer =>
            {
                var trigger = component.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = component.gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.FixedUpdateAsObservable().Subscribe(observer);
            });
        }

        /// <summary>
        /// Observes Unity's LateUpdate loop.
        /// </summary>
        /// <param name="component">The component to observe.</param>
        /// <returns>An observable that emits every frame during LateUpdate.</returns>
        public static IObservable<Unit> LateUpdateAsObservable(this Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            return Observable.Create<Unit>(observer =>
            {
                var trigger = component.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = component.gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.LateUpdateAsObservable().Subscribe(observer);
            });
        }

        /// <summary>
        /// Creates an observable that emits every frame during Update.
        /// </summary>
        /// <returns>An observable that emits every frame.</returns>
        public static IObservable<Unit> EveryUpdate()
        {
            return Observable.Create<Unit>(observer =>
            {
                var runner = UnitySchedulerBase.Runner;
                var trigger = runner.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = runner.gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.UpdateAsObservable().Subscribe(observer);
            });
        }

        /// <summary>
        /// Creates an observable that emits every fixed frame during FixedUpdate.
        /// </summary>
        /// <returns>An observable that emits every fixed frame.</returns>
        public static IObservable<Unit> EveryFixedUpdate()
        {
            return Observable.Create<Unit>(observer =>
            {
                var runner = UnitySchedulerBase.Runner;
                var trigger = runner.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = runner.gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.FixedUpdateAsObservable().Subscribe(observer);
            });
        }

        /// <summary>
        /// Creates an observable that emits every frame during LateUpdate.
        /// </summary>
        /// <returns>An observable that emits every frame.</returns>
        public static IObservable<Unit> EveryLateUpdate()
        {
            return Observable.Create<Unit>(observer =>
            {
                var runner = UnitySchedulerBase.Runner;
                var trigger = runner.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = runner.gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.LateUpdateAsObservable().Subscribe(observer);
            });
        }

        /// <summary>
        /// Converts an observable sequence to a coroutine.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The observable sequence to convert.</param>
        /// <param name="onNext">Action to invoke for each element.</param>
        /// <param name="onError">Action to invoke upon exceptional termination.</param>
        /// <returns>A coroutine that can be started with StartCoroutine.</returns>
        public static IEnumerator ToCoroutine<T>(
            this IObservable<T> source,
            Action<T> onNext = null,
            Action<Exception> onError = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var completed = false;
            var error = default(Exception);

            var subscription = source.Subscribe(Observer.Create<T>(
                value => onNext?.Invoke(value),
                ex =>
                {
                    error = ex;
                    completed = true;
                    onError?.Invoke(ex);
                },
                () => completed = true
            ));

            try
            {
                while (!completed)
                {
                    yield return null;
                }

                if (error != null && onError == null)
                {
                    Debug.LogError($"[Ludo.Reactive] Observable coroutine terminated with error: {error}");
                }
            }
            finally
            {
                subscription?.Dispose();
            }
        }

        /// <summary>
        /// Returns elements from an observable sequence until the other observable sequence produces a value.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TOther">The type of the elements in the other sequence that terminates propagation of elements of the source sequence.</typeparam>
        /// <param name="source">Source sequence to propagate elements for.</param>
        /// <param name="other">Observable sequence that terminates propagation of elements of the source sequence.</param>
        /// <returns>An observable sequence containing the elements of the source sequence up to the point the other sequence interrupted further propagation.</returns>
        public static IObservable<T> TakeUntil<T, TOther>(
            this IObservable<T> source,
            IObservable<TOther> other)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return Observable.Create<T>(observer =>
            {
                var composite = new CompositeDisposable();

                composite.Add(other.Subscribe(Observer.Create<TOther>(
                    _ =>
                    {
                        observer.OnCompleted();
                        composite.Dispose(); // Dispose all subscriptions when terminating
                    },
                    observer.OnError,
                    () => { }
                )));

                composite.Add(source.Subscribe(observer));

                return composite;
            });
        }

        /// <summary>
        /// Takes elements from the observable sequence until the specified GameObject is destroyed.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to propagate elements for.</param>
        /// <param name="gameObject">GameObject whose destruction terminates the sequence.</param>
        /// <returns>An observable sequence containing the elements of the source sequence up to the point the GameObject is destroyed.</returns>
        public static IObservable<T> TakeUntilDestroy<T>(
            this IObservable<T> source,
            GameObject gameObject)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            return source.TakeUntil(gameObject.OnDestroyAsObservable());
        }

        /// <summary>
        /// Takes elements from the observable sequence until the specified Component is destroyed.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to propagate elements for.</param>
        /// <param name="component">Component whose destruction terminates the sequence.</param>
        /// <returns>An observable sequence containing the elements of the source sequence up to the point the Component is destroyed.</returns>
        public static IObservable<T> TakeUntilDestroy<T>(
            this IObservable<T> source,
            Component component)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            return source.TakeUntil(component.OnDestroyAsObservable());
        }

        /// <summary>
        /// Delays the observable sequence by the specified number of frames.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to delay.</param>
        /// <param name="frameCount">Number of frames to delay.</param>
        /// <returns>An observable sequence that is delayed by the specified number of frames.</returns>
        public static IObservable<T> DelayFrame<T>(
            this IObservable<T> source,
            int frameCount)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (frameCount < 0)
                throw new ArgumentOutOfRangeException(nameof(frameCount));

            if (frameCount == 0)
                return source;

            return Observable.Create<T>(observer =>
            {
                return source.Subscribe(Observer.Create<T>(
                    value =>
                    {
                        var runner = UnitySchedulerBase.Runner;
                        runner.StartCoroutine(DelayFrameCoroutine(frameCount, () => observer.OnNext(value)));
                    },
                    observer.OnError,
                    observer.OnCompleted
                ));
            });
        }

        /// <summary>
        /// Converts an observable sequence to a yield instruction that can be used in coroutines.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The observable sequence to convert.</param>
        /// <param name="onNext">Action to invoke for each element.</param>
        /// <param name="onError">Action to invoke upon exceptional termination.</param>
        /// <returns>A yield instruction that completes when the observable sequence completes.</returns>
        public static IEnumerator ToYieldInstruction<T>(
            this IObservable<T> source,
            Action<T> onNext = null,
            Action<Exception> onError = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.ToCoroutine(onNext, onError);
        }

        private static IEnumerator DelayFrameCoroutine(int frameCount, Action action)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
            }
            action?.Invoke();
        }

        /// <summary>
        /// Observes the OnDestroy event of the specified GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to observe.</param>
        /// <returns>An observable that emits when the GameObject is destroyed.</returns>
        public static IObservable<Unit> OnDestroyAsObservable(this GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            return Observable.Create<Unit>(observer =>
            {
                var trigger = gameObject.GetComponent<ObservableTrigger>();
                if (trigger == null)
                {
                    trigger = gameObject.AddComponent<ObservableTrigger>();
                }

                return trigger.OnDestroyAsObservable().Subscribe(observer);
            });
        }
    }
}
