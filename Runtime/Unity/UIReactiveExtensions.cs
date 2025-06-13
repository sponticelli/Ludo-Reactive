using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ludo.Reactive
{
    /// <summary>
    /// Provides enhanced reactive extensions for Unity UI components with advanced binding capabilities.
    /// Extends beyond basic components to include advanced Slider, Toggle, Dropdown, and ScrollRect integrations.
    /// </summary>
    public static class UIReactiveExtensions
    {
        /// <summary>
        /// Creates a two-way reactive binding for a Slider component.
        /// </summary>
        /// <param name="slider">The Slider to bind.</param>
        /// <param name="property">The reactive property to bind to.</param>
        /// <returns>A disposable that manages the binding.</returns>
        public static IDisposable BindToReactiveProperty(this Slider slider, ReactiveProperty<float> property)
        {
            if (slider == null)
                throw new ArgumentNullException(nameof(slider));
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            // Set initial value
            slider.value = property.Value;

            var disposables = ReactiveObjectPools.GetDisposableList();

            // Bind property changes to slider
            disposables.Add(property.Subscribe(value =>
            {
                if (Math.Abs(slider.value - value) > 0.001f)
                {
                    slider.value = value;
                }
            }));

            // Bind slider changes to property
            disposables.Add(slider.onValueChanged.AsObservable().Subscribe(value =>
            {
                if (Math.Abs(property.Value - value) > 0.001f)
                {
                    property.Value = value;
                }
            }));

            return Disposable.Create(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
                ReactiveObjectPools.ReturnDisposableList(disposables);
            });
        }

        /// <summary>
        /// Creates a reactive binding for a Slider with value transformation.
        /// </summary>
        /// <typeparam name="T">The type of the source property.</typeparam>
        /// <param name="slider">The Slider to bind.</param>
        /// <param name="property">The reactive property to bind to.</param>
        /// <param name="toSlider">Function to convert from property value to slider value.</param>
        /// <param name="fromSlider">Function to convert from slider value to property value.</param>
        /// <returns>A disposable that manages the binding.</returns>
        public static IDisposable BindToReactiveProperty<T>(this Slider slider, ReactiveProperty<T> property, 
            Func<T, float> toSlider, Func<float, T> fromSlider)
        {
            if (slider == null)
                throw new ArgumentNullException(nameof(slider));
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            if (toSlider == null)
                throw new ArgumentNullException(nameof(toSlider));
            if (fromSlider == null)
                throw new ArgumentNullException(nameof(fromSlider));

            // Set initial value
            slider.value = toSlider(property.Value);

            var disposables = ReactiveObjectPools.GetDisposableList();

            // Bind property changes to slider
            disposables.Add(property.Subscribe(value =>
            {
                var sliderValue = toSlider(value);
                if (Math.Abs(slider.value - sliderValue) > 0.001f)
                {
                    slider.value = sliderValue;
                }
            }));

            // Bind slider changes to property
            disposables.Add(slider.onValueChanged.AsObservable().Subscribe(sliderValue =>
            {
                var propertyValue = fromSlider(sliderValue);
                if (!EqualityComparer<T>.Default.Equals(property.Value, propertyValue))
                {
                    property.Value = propertyValue;
                }
            }));

            return Disposable.Create(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
                ReactiveObjectPools.ReturnDisposableList(disposables);
            });
        }

        /// <summary>
        /// Creates a two-way reactive binding for a Toggle component with state management.
        /// </summary>
        /// <param name="toggle">The Toggle to bind.</param>
        /// <param name="property">The reactive property to bind to.</param>
        /// <returns>A disposable that manages the binding.</returns>
        public static IDisposable BindToReactiveProperty(this Toggle toggle, ReactiveProperty<bool> property)
        {
            if (toggle == null)
                throw new ArgumentNullException(nameof(toggle));
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            // Set initial value
            toggle.isOn = property.Value;

            var disposables = ReactiveObjectPools.GetDisposableList();

            // Bind property changes to toggle
            disposables.Add(property.Subscribe(value =>
            {
                if (toggle.isOn != value)
                {
                    toggle.isOn = value;
                }
            }));

            // Bind toggle changes to property
            disposables.Add(toggle.onValueChanged.AsObservable().Subscribe(value =>
            {
                if (property.Value != value)
                {
                    property.Value = value;
                }
            }));

            return Disposable.Create(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
                ReactiveObjectPools.ReturnDisposableList(disposables);
            });
        }

        /// <summary>
        /// Creates an enhanced observable for Dropdown selection changes with option data.
        /// </summary>
        /// <param name="dropdown">The Dropdown to observe.</param>
        /// <returns>An observable that emits dropdown selection data.</returns>
        public static IObservable<DropdownSelectionData> OnSelectionChangedAsObservable(this Dropdown dropdown)
        {
            if (dropdown == null)
                throw new ArgumentNullException(nameof(dropdown));

            return Observable.Create<DropdownSelectionData>(observer =>
            {
                // Send initial value
                var initialData = new DropdownSelectionData
                {
                    Index = dropdown.value,
                    Text = dropdown.value < dropdown.options.Count ? dropdown.options[dropdown.value].text : string.Empty,
                    Image = dropdown.value < dropdown.options.Count ? dropdown.options[dropdown.value].image : null
                };
                observer.OnNext(initialData);

                // Subscribe to changes
                return dropdown.onValueChanged.AsObservable().Subscribe(index =>
                {
                    var data = new DropdownSelectionData
                    {
                        Index = index,
                        Text = index < dropdown.options.Count ? dropdown.options[index].text : string.Empty,
                        Image = index < dropdown.options.Count ? dropdown.options[index].image : null
                    };
                    observer.OnNext(data);
                });
            });
        }

        /// <summary>
        /// Creates a reactive binding for a Dropdown with enum values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="dropdown">The Dropdown to bind.</param>
        /// <param name="property">The reactive property to bind to.</param>
        /// <returns>A disposable that manages the binding.</returns>
        public static IDisposable BindToReactiveProperty<TEnum>(this Dropdown dropdown, ReactiveProperty<TEnum> property)
            where TEnum : Enum
        {
            if (dropdown == null)
                throw new ArgumentNullException(nameof(dropdown));
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            // Set initial value
            dropdown.value = Convert.ToInt32(property.Value);

            var disposables = ReactiveObjectPools.GetDisposableList();

            // Bind property changes to dropdown
            disposables.Add(property.Subscribe(value =>
            {
                var index = Convert.ToInt32(value);
                if (dropdown.value != index)
                {
                    dropdown.value = index;
                }
            }));

            // Bind dropdown changes to property
            disposables.Add(dropdown.onValueChanged.AsObservable().Subscribe(index =>
            {
                var enumValue = (TEnum)Enum.ToObject(typeof(TEnum), index);
                if (!EqualityComparer<TEnum>.Default.Equals(property.Value, enumValue))
                {
                    property.Value = enumValue;
                }
            }));

            return Disposable.Create(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
                ReactiveObjectPools.ReturnDisposableList(disposables);
            });
        }

        /// <summary>
        /// Creates an enhanced observable for ScrollRect position and content changes.
        /// </summary>
        /// <param name="scrollRect">The ScrollRect to observe.</param>
        /// <returns>An observable that emits scroll data.</returns>
        public static IObservable<ScrollRectData> OnScrollAsObservable(this ScrollRect scrollRect)
        {
            if (scrollRect == null)
                throw new ArgumentNullException(nameof(scrollRect));

            return Observable.Create<ScrollRectData>(observer =>
            {
                var disposables = ReactiveObjectPools.GetDisposableList();

                // Send initial value
                var initialData = CreateScrollRectData(scrollRect);
                observer.OnNext(initialData);

                // Subscribe to position changes
                disposables.Add(scrollRect.onValueChanged.AsObservable().Subscribe(position =>
                {
                    var data = CreateScrollRectData(scrollRect, position);
                    observer.OnNext(data);
                }));

                return Disposable.Create(() =>
                {
                    foreach (var disposable in disposables)
                    {
                        disposable.Dispose();
                    }
                    ReactiveObjectPools.ReturnDisposableList(disposables);
                });
            });
        }

        /// <summary>
        /// Creates an observable that emits when the ScrollRect reaches the end (for infinite scrolling).
        /// </summary>
        /// <param name="scrollRect">The ScrollRect to observe.</param>
        /// <param name="threshold">The threshold distance from the end to trigger (0.0 to 1.0).</param>
        /// <returns>An observable that emits when near the end.</returns>
        public static IObservable<Unit> OnNearEndAsObservable(this ScrollRect scrollRect, float threshold = 0.1f)
        {
            if (scrollRect == null)
                throw new ArgumentNullException(nameof(scrollRect));

            return scrollRect.onValueChanged.AsObservable()
                .Where(position =>
                {
                    if (scrollRect.vertical)
                        return position.y <= threshold;
                    else
                        return position.x >= (1.0f - threshold);
                })
                .Select(_ => Unit.Default);
        }

        private static ScrollRectData CreateScrollRectData(ScrollRect scrollRect)
        {
            return CreateScrollRectData(scrollRect, scrollRect.normalizedPosition);
        }

        private static ScrollRectData CreateScrollRectData(ScrollRect scrollRect, Vector2 normalizedPosition)
        {
            var contentBounds = scrollRect.content != null ? scrollRect.content.rect : Rect.zero;
            var viewportBounds = scrollRect.viewport != null ? scrollRect.viewport.rect : scrollRect.GetComponent<RectTransform>().rect;

            return new ScrollRectData
            {
                NormalizedPosition = normalizedPosition,
                ContentPosition = scrollRect.content != null ? scrollRect.content.anchoredPosition : Vector2.zero,
                ContentSize = contentBounds.size,
                ViewportSize = viewportBounds.size,
                Velocity = scrollRect.velocity,
                IsAtTop = normalizedPosition.y >= 0.99f,
                IsAtBottom = normalizedPosition.y <= 0.01f,
                IsAtLeft = normalizedPosition.x <= 0.01f,
                IsAtRight = normalizedPosition.x >= 0.99f
            };
        }
    }

    /// <summary>
    /// Data structure for dropdown selection information.
    /// </summary>
    public struct DropdownSelectionData
    {
        public int Index;
        public string Text;
        public Sprite Image;
    }

    /// <summary>
    /// Data structure for scroll rect information.
    /// </summary>
    public struct ScrollRectData
    {
        public Vector2 NormalizedPosition;
        public Vector2 ContentPosition;
        public Vector2 ContentSize;
        public Vector2 ViewportSize;
        public Vector2 Velocity;
        public bool IsAtTop;
        public bool IsAtBottom;
        public bool IsAtLeft;
        public bool IsAtRight;
    }
}
