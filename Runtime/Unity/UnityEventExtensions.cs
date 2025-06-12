using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludo.Reactive
{
    /// <summary>
    /// Provides extension methods for converting Unity Events to observable sequences.
    /// </summary>
    public static class UnityEventExtensions
    {
        /// <summary>
        /// Converts a UnityEvent to an observable sequence.
        /// </summary>
        /// <param name="unityEvent">The UnityEvent to convert.</param>
        /// <returns>An observable sequence that emits when the UnityEvent is invoked.</returns>
        public static IObservable<Unit> AsObservable(this UnityEvent unityEvent)
        {
            if (unityEvent == null)
                throw new ArgumentNullException(nameof(unityEvent));

            return Observable.Create<Unit>(observer =>
            {
                UnityAction handler = () => observer.OnNext(Unit.Default);
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
        public static IObservable<T> AsObservable<T>(this UnityEvent<T> unityEvent)
        {
            if (unityEvent == null)
                throw new ArgumentNullException(nameof(unityEvent));

            return Observable.Create<T>(observer =>
            {
                UnityAction<T> handler = value => observer.OnNext(value);
                unityEvent.AddListener(handler);
                return Disposable.Create(() => unityEvent.RemoveListener(handler));
            });
        }

        /// <summary>
        /// Converts a UnityEvent&lt;T0, T1&gt; to an observable sequence.
        /// </summary>
        /// <typeparam name="T0">The type of the first event argument.</typeparam>
        /// <typeparam name="T1">The type of the second event argument.</typeparam>
        /// <param name="unityEvent">The UnityEvent to convert.</param>
        /// <returns>An observable sequence that emits tuples of the event arguments when the UnityEvent is invoked.</returns>
        public static IObservable<(T0, T1)> AsObservable<T0, T1>(this UnityEvent<T0, T1> unityEvent)
        {
            if (unityEvent == null)
                throw new ArgumentNullException(nameof(unityEvent));

            return Observable.Create<(T0, T1)>(observer =>
            {
                UnityAction<T0, T1> handler = (arg0, arg1) => observer.OnNext((arg0, arg1));
                unityEvent.AddListener(handler);
                return Disposable.Create(() => unityEvent.RemoveListener(handler));
            });
        }

        /// <summary>
        /// Converts a UnityEvent&lt;T0, T1, T2&gt; to an observable sequence.
        /// </summary>
        /// <typeparam name="T0">The type of the first event argument.</typeparam>
        /// <typeparam name="T1">The type of the second event argument.</typeparam>
        /// <typeparam name="T2">The type of the third event argument.</typeparam>
        /// <param name="unityEvent">The UnityEvent to convert.</param>
        /// <returns>An observable sequence that emits tuples of the event arguments when the UnityEvent is invoked.</returns>
        public static IObservable<(T0, T1, T2)> AsObservable<T0, T1, T2>(this UnityEvent<T0, T1, T2> unityEvent)
        {
            if (unityEvent == null)
                throw new ArgumentNullException(nameof(unityEvent));

            return Observable.Create<(T0, T1, T2)>(observer =>
            {
                UnityAction<T0, T1, T2> handler = (arg0, arg1, arg2) => observer.OnNext((arg0, arg1, arg2));
                unityEvent.AddListener(handler);
                return Disposable.Create(() => unityEvent.RemoveListener(handler));
            });
        }

        /// <summary>
        /// Converts a UnityEvent&lt;T0, T1, T2, T3&gt; to an observable sequence.
        /// </summary>
        /// <typeparam name="T0">The type of the first event argument.</typeparam>
        /// <typeparam name="T1">The type of the second event argument.</typeparam>
        /// <typeparam name="T2">The type of the third event argument.</typeparam>
        /// <typeparam name="T3">The type of the fourth event argument.</typeparam>
        /// <param name="unityEvent">The UnityEvent to convert.</param>
        /// <returns>An observable sequence that emits tuples of the event arguments when the UnityEvent is invoked.</returns>
        public static IObservable<(T0, T1, T2, T3)> AsObservable<T0, T1, T2, T3>(this UnityEvent<T0, T1, T2, T3> unityEvent)
        {
            if (unityEvent == null)
                throw new ArgumentNullException(nameof(unityEvent));

            return Observable.Create<(T0, T1, T2, T3)>(observer =>
            {
                UnityAction<T0, T1, T2, T3> handler = (arg0, arg1, arg2, arg3) => observer.OnNext((arg0, arg1, arg2, arg3));
                unityEvent.AddListener(handler);
                return Disposable.Create(() => unityEvent.RemoveListener(handler));
            });
        }
    }

    /// <summary>
    /// Provides extension methods for UI components to create observable sequences.
    /// </summary>
    public static class UIObservableExtensions
    {
        /// <summary>
        /// Converts a Button's onClick event to an observable sequence.
        /// </summary>
        /// <param name="button">The Button to observe.</param>
        /// <returns>An observable sequence that emits when the button is clicked.</returns>
        public static IObservable<Unit> OnClickAsObservable(this UnityEngine.UI.Button button)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button));

            return button.onClick.AsObservable();
        }

        /// <summary>
        /// Converts a Toggle's onValueChanged event to an observable sequence.
        /// </summary>
        /// <param name="toggle">The Toggle to observe.</param>
        /// <returns>An observable sequence that emits the toggle value when it changes.</returns>
        public static IObservable<bool> OnValueChangedAsObservable(this UnityEngine.UI.Toggle toggle)
        {
            if (toggle == null)
                throw new ArgumentNullException(nameof(toggle));

            return toggle.onValueChanged.AsObservable();
        }

        /// <summary>
        /// Converts a Slider's onValueChanged event to an observable sequence.
        /// </summary>
        /// <param name="slider">The Slider to observe.</param>
        /// <returns>An observable sequence that emits the slider value when it changes.</returns>
        public static IObservable<float> OnValueChangedAsObservable(this UnityEngine.UI.Slider slider)
        {
            if (slider == null)
                throw new ArgumentNullException(nameof(slider));

            return slider.onValueChanged.AsObservable();
        }

        /// <summary>
        /// Converts an InputField's onValueChanged event to an observable sequence.
        /// </summary>
        /// <param name="inputField">The InputField to observe.</param>
        /// <returns>An observable sequence that emits the input field text when it changes.</returns>
        public static IObservable<string> OnValueChangedAsObservable(this UnityEngine.UI.InputField inputField)
        {
            if (inputField == null)
                throw new ArgumentNullException(nameof(inputField));

            return inputField.onValueChanged.AsObservable();
        }

        /// <summary>
        /// Converts an InputField's onEndEdit event to an observable sequence.
        /// </summary>
        /// <param name="inputField">The InputField to observe.</param>
        /// <returns>An observable sequence that emits the input field text when editing ends.</returns>
        public static IObservable<string> OnEndEditAsObservable(this UnityEngine.UI.InputField inputField)
        {
            if (inputField == null)
                throw new ArgumentNullException(nameof(inputField));

            return inputField.onEndEdit.AsObservable();
        }

        /// <summary>
        /// Converts a Dropdown's onValueChanged event to an observable sequence.
        /// </summary>
        /// <param name="dropdown">The Dropdown to observe.</param>
        /// <returns>An observable sequence that emits the dropdown value when it changes.</returns>
        public static IObservable<int> OnValueChangedAsObservable(this UnityEngine.UI.Dropdown dropdown)
        {
            if (dropdown == null)
                throw new ArgumentNullException(nameof(dropdown));

            return dropdown.onValueChanged.AsObservable();
        }

        /// <summary>
        /// Converts a Scrollbar's onValueChanged event to an observable sequence.
        /// </summary>
        /// <param name="scrollbar">The Scrollbar to observe.</param>
        /// <returns>An observable sequence that emits the scrollbar value when it changes.</returns>
        public static IObservable<float> OnValueChangedAsObservable(this UnityEngine.UI.Scrollbar scrollbar)
        {
            if (scrollbar == null)
                throw new ArgumentNullException(nameof(scrollbar));

            return scrollbar.onValueChanged.AsObservable();
        }

        /// <summary>
        /// Converts a ScrollRect's onValueChanged event to an observable sequence.
        /// </summary>
        /// <param name="scrollRect">The ScrollRect to observe.</param>
        /// <returns>An observable sequence that emits the scroll position when it changes.</returns>
        public static IObservable<Vector2> OnValueChangedAsObservable(this UnityEngine.UI.ScrollRect scrollRect)
        {
            if (scrollRect == null)
                throw new ArgumentNullException(nameof(scrollRect));

            return scrollRect.onValueChanged.AsObservable();
        }
    }
}
