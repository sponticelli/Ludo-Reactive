using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Unit tests for UI reactive extensions.
    /// </summary>
    public class UIReactiveExtensionsTests
    {
        private GameObject _testGameObject;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestUI");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        [Test]
        public void UIReactiveExtensions_SliderBindToReactiveProperty_Works()
        {
            // Arrange
            var slider = _testGameObject.AddComponent<Slider>();
            var property = new ReactiveProperty<float>(0.5f);

            // Act
            var binding = slider.BindToReactiveProperty(property);

            // Assert
            Assert.AreEqual(0.5f, slider.value, 0.001f);

            // Test property to slider
            property.Value = 0.8f;
            Assert.AreEqual(0.8f, slider.value, 0.001f);

            // Test slider to property
            slider.value = 0.3f;
            slider.onValueChanged.Invoke(0.3f);
            Assert.AreEqual(0.3f, property.Value, 0.001f);

            // Cleanup
            binding.Dispose();
            property.Dispose();
        }

        [Test]
        public void UIReactiveExtensions_SliderBindWithTransformation_Works()
        {
            // Arrange
            var slider = _testGameObject.AddComponent<Slider>();
            var property = new ReactiveProperty<int>(50);

            // Act - Bind with transformation (int 0-100 to float 0.0-1.0)
            var binding = slider.BindToReactiveProperty(
                property,
                intValue => intValue / 100f,
                floatValue => Mathf.RoundToInt(floatValue * 100f)
            );

            // Assert
            Assert.AreEqual(0.5f, slider.value, 0.001f);

            // Test property to slider
            property.Value = 80;
            Assert.AreEqual(0.8f, slider.value, 0.001f);

            // Test slider to property
            slider.value = 0.3f;
            slider.onValueChanged.Invoke(0.3f);
            Assert.AreEqual(30, property.Value);

            // Cleanup
            binding.Dispose();
            property.Dispose();
        }

        [Test]
        public void UIReactiveExtensions_ToggleBindToReactiveProperty_Works()
        {
            // Arrange
            var toggle = _testGameObject.AddComponent<Toggle>();
            var property = new ReactiveProperty<bool>(true);

            // Act
            var binding = toggle.BindToReactiveProperty(property);

            // Assert
            Assert.IsTrue(toggle.isOn);

            // Test property to toggle
            property.Value = false;
            Assert.IsFalse(toggle.isOn);

            // Test toggle to property
            toggle.isOn = true;
            toggle.onValueChanged.Invoke(true);
            Assert.IsTrue(property.Value);

            // Cleanup
            binding.Dispose();
            property.Dispose();
        }

        [Test]
        public void UIReactiveExtensions_DropdownOnSelectionChanged_Works()
        {
            // Arrange
            var dropdown = _testGameObject.AddComponent<Dropdown>();
            dropdown.options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Option 1"),
                new Dropdown.OptionData("Option 2"),
                new Dropdown.OptionData("Option 3")
            };
            dropdown.value = 0;

            var receivedData = new List<DropdownSelectionData>();

            // Act
            var subscription = dropdown.OnSelectionChangedAsObservable()
                .Subscribe(data => receivedData.Add(data));

            // Trigger change - only invoke manually, don't set value first
            dropdown.onValueChanged.Invoke(1);

            // Assert
            Assert.AreEqual(2, receivedData.Count); // Initial + change
            Assert.AreEqual(0, receivedData[0].Index);
            Assert.AreEqual("Option 1", receivedData[0].Text);
            Assert.AreEqual(1, receivedData[1].Index);
            Assert.AreEqual("Option 2", receivedData[1].Text);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void UIReactiveExtensions_DropdownBindToEnum_Works()
        {
            // Arrange
            var dropdown = _testGameObject.AddComponent<Dropdown>();
            dropdown.options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Small"),
                new Dropdown.OptionData("Medium"),
                new Dropdown.OptionData("Large")
            };

            var property = new ReactiveProperty<TestSize>(TestSize.Medium);

            // Act
            var binding = dropdown.BindToReactiveProperty(property);

            // Assert
            Assert.AreEqual(1, dropdown.value); // Medium = 1

            // Test property to dropdown
            property.Value = TestSize.Large;
            Assert.AreEqual(2, dropdown.value);

            // Test dropdown to property
            dropdown.value = 0;
            dropdown.onValueChanged.Invoke(0);
            Assert.AreEqual(TestSize.Small, property.Value);

            // Cleanup
            binding.Dispose();
            property.Dispose();
        }

        [Test]
        public void UIReactiveExtensions_ScrollRectOnScroll_Works()
        {
            // Arrange
            var scrollRect = _testGameObject.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport").AddComponent<RectTransform>();
            var content = new GameObject("Content").AddComponent<RectTransform>();

            viewport.transform.SetParent(_testGameObject.transform);
            content.transform.SetParent(viewport.transform);

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.normalizedPosition = Vector2.zero;

            var receivedData = new List<ScrollRectData>();

            // Act
            var subscription = scrollRect.OnScrollAsObservable()
                .Subscribe(data => receivedData.Add(data));

            // Trigger scroll - manually invoke with desired position
            scrollRect.onValueChanged.Invoke(new Vector2(0.5f, 0.5f));

            // Assert
            Assert.AreEqual(2, receivedData.Count); // Initial + change
            Assert.AreEqual(Vector2.zero, receivedData[0].NormalizedPosition);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), receivedData[1].NormalizedPosition);

            // Cleanup
            subscription.Dispose();
            UnityEngine.Object.DestroyImmediate(viewport.gameObject);
        }

        [Test]
        public void UIReactiveExtensions_ScrollRectOnNearEnd_Works()
        {
            // Arrange
            var scrollRect = _testGameObject.AddComponent<ScrollRect>();
            scrollRect.vertical = true;
            
            var nearEndCount = 0;

            // Act
            var subscription = scrollRect.OnNearEndAsObservable(0.1f)
                .Subscribe(_ => nearEndCount++);

            // Test not near end
            scrollRect.onValueChanged.Invoke(new Vector2(0f, 0.5f));
            Assert.AreEqual(0, nearEndCount);

            // Test near end
            scrollRect.onValueChanged.Invoke(new Vector2(0f, 0.05f));
            Assert.AreEqual(1, nearEndCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void UIReactiveExtensions_NullArguments_ThrowExceptions()
        {
            // Arrange
            Slider slider = null;
            Toggle toggle = null;
            Dropdown dropdown = null;
            ScrollRect scrollRect = null;
            ReactiveProperty<float> floatProperty = null;
            ReactiveProperty<bool> boolProperty = null;
            ReactiveProperty<TestSize> enumProperty = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => slider.BindToReactiveProperty(new ReactiveProperty<float>()));
            Assert.Throws<ArgumentNullException>(() => _testGameObject.AddComponent<Slider>().BindToReactiveProperty(floatProperty));
            
            Assert.Throws<ArgumentNullException>(() => toggle.BindToReactiveProperty(new ReactiveProperty<bool>()));
            Assert.Throws<ArgumentNullException>(() => _testGameObject.AddComponent<Toggle>().BindToReactiveProperty(boolProperty));
            
            Assert.Throws<ArgumentNullException>(() => dropdown.OnSelectionChangedAsObservable());
            Assert.Throws<ArgumentNullException>(() => dropdown.BindToReactiveProperty(new ReactiveProperty<TestSize>()));
            Assert.Throws<ArgumentNullException>(() => _testGameObject.AddComponent<Dropdown>().BindToReactiveProperty(enumProperty));
            
            Assert.Throws<ArgumentNullException>(() => scrollRect.OnScrollAsObservable());
            Assert.Throws<ArgumentNullException>(() => scrollRect.OnNearEndAsObservable());
        }

        [Test]
        public void UIReactiveExtensions_SliderBindingPreventsInfiniteLoop()
        {
            // Arrange
            var slider = _testGameObject.AddComponent<Slider>();
            var property = new ReactiveProperty<float>(0.5f);
            var changeCount = 0;

            property.Subscribe(_ => changeCount++);

            // Act
            var binding = slider.BindToReactiveProperty(property);

            // Set the same value multiple times
            property.Value = 0.5f;
            property.Value = 0.5f;
            slider.value = 0.5f;
            slider.onValueChanged.Invoke(0.5f);

            // Assert - Should not cause infinite loop or excessive notifications
            Assert.LessOrEqual(changeCount, 3); // Initial + at most 2 changes

            // Cleanup
            binding.Dispose();
            property.Dispose();
        }

        [Test]
        public void UIReactiveExtensions_ToggleBindingPreventsInfiniteLoop()
        {
            // Arrange
            var toggle = _testGameObject.AddComponent<Toggle>();
            var property = new ReactiveProperty<bool>(true);
            var changeCount = 0;

            property.Subscribe(_ => changeCount++);

            // Act
            var binding = toggle.BindToReactiveProperty(property);

            // Set the same value multiple times
            property.Value = true;
            property.Value = true;
            toggle.isOn = true;
            toggle.onValueChanged.Invoke(true);

            // Assert - Should not cause infinite loop or excessive notifications
            Assert.LessOrEqual(changeCount, 3); // Initial + at most 2 changes

            // Cleanup
            binding.Dispose();
            property.Dispose();
        }

        [Test]
        public void ScrollRectData_BoundaryDetection_Works()
        {
            // Arrange
            var scrollRect = _testGameObject.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport").AddComponent<RectTransform>();
            var content = new GameObject("Content").AddComponent<RectTransform>();

            viewport.transform.SetParent(_testGameObject.transform);
            content.transform.SetParent(viewport.transform);

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            var receivedData = new List<ScrollRectData>();

            // Act
            var subscription = scrollRect.OnScrollAsObservable()
                .Subscribe(data => receivedData.Add(data));

            // Test different positions
            scrollRect.onValueChanged.Invoke(new Vector2(0f, 1f)); // Top-left

            scrollRect.onValueChanged.Invoke(new Vector2(1f, 0f)); // Bottom-right

            // Assert
            Assert.IsTrue(receivedData[1].IsAtTop);
            Assert.IsTrue(receivedData[1].IsAtLeft);
            Assert.IsFalse(receivedData[1].IsAtBottom);
            Assert.IsFalse(receivedData[1].IsAtRight);

            Assert.IsFalse(receivedData[2].IsAtTop);
            Assert.IsFalse(receivedData[2].IsAtLeft);
            Assert.IsTrue(receivedData[2].IsAtBottom);
            Assert.IsTrue(receivedData[2].IsAtRight);

            // Cleanup
            subscription.Dispose();
            UnityEngine.Object.DestroyImmediate(viewport.gameObject);
        }

        // Test enum for dropdown binding
        private enum TestSize
        {
            Small = 0,
            Medium = 1,
            Large = 2
        }
    }
}
