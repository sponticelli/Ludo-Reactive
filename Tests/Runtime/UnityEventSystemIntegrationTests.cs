using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Integration tests for Unity event system integration with reactive system.
    /// </summary>
    public class UnityEventSystemIntegrationTests
    {
        private GameObject _testGameObject;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestEventSystem");
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
        public void UnityEvent_AsObservable_EmitsOnInvoke()
        {
            // Arrange
            var unityEvent = new UnityEvent();
            var emissionCount = 0;

            // Act
            var subscription = unityEvent.AsObservable()
                .Subscribe(_ => emissionCount++);

            unityEvent.Invoke();
            unityEvent.Invoke();
            unityEvent.Invoke();

            // Assert
            Assert.AreEqual(3, emissionCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void UnityEventGeneric_AsObservable_EmitsValues()
        {
            // Arrange
            var unityEvent = new UnityEvent<string>();
            var receivedValues = new List<string>();

            // Act
            var subscription = unityEvent.AsObservable()
                .Subscribe(value => receivedValues.Add(value));

            unityEvent.Invoke("Hello");
            unityEvent.Invoke("World");
            unityEvent.Invoke("Test");

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { "Hello", "World", "Test" }, receivedValues.ToArray());

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void UnityEventInt_AsObservable_EmitsIntValues()
        {
            // Arrange
            var unityEvent = new UnityEvent<int>();
            var receivedValues = new List<int>();

            // Act
            var subscription = unityEvent.AsObservable()
                .Subscribe(value => receivedValues.Add(value));

            unityEvent.Invoke(1);
            unityEvent.Invoke(42);
            unityEvent.Invoke(-10);

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 42, -10 }, receivedValues.ToArray());

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void UnityEventFloat_AsObservable_EmitsFloatValues()
        {
            // Arrange
            var unityEvent = new UnityEvent<float>();
            var receivedValues = new List<float>();

            // Act
            var subscription = unityEvent.AsObservable()
                .Subscribe(value => receivedValues.Add(value));

            unityEvent.Invoke(1.5f);
            unityEvent.Invoke(3.14f);
            unityEvent.Invoke(-2.7f);

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(1.5f, receivedValues[0], 0.001f);
            Assert.AreEqual(3.14f, receivedValues[1], 0.001f);
            Assert.AreEqual(-2.7f, receivedValues[2], 0.001f);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void UnityEventBool_AsObservable_EmitsBoolValues()
        {
            // Arrange
            var unityEvent = new UnityEvent<bool>();
            var receivedValues = new List<bool>();

            // Act
            var subscription = unityEvent.AsObservable()
                .Subscribe(value => receivedValues.Add(value));

            unityEvent.Invoke(true);
            unityEvent.Invoke(false);
            unityEvent.Invoke(true);

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { true, false, true }, receivedValues.ToArray());

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void UnityEventVector2_AsObservable_EmitsVector2Values()
        {
            // Arrange
            var unityEvent = new UnityEvent<Vector2>();
            var receivedValues = new List<Vector2>();

            // Act
            var subscription = unityEvent.AsObservable()
                .Subscribe(value => receivedValues.Add(value));

            unityEvent.Invoke(Vector2.zero);
            unityEvent.Invoke(Vector2.one);
            unityEvent.Invoke(new Vector2(3, 4));

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(Vector2.zero, receivedValues[0]);
            Assert.AreEqual(Vector2.one, receivedValues[1]);
            Assert.AreEqual(new Vector2(3, 4), receivedValues[2]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void ButtonOnClick_AsObservable_EmitsOnClick()
        {
            // Arrange
            var button = _testGameObject.AddComponent<Button>();
            var clickCount = 0;

            // Act
            var subscription = button.OnClickAsObservable()
                .Subscribe(_ => clickCount++);

            // Simulate button clicks
            button.onClick.Invoke();
            button.onClick.Invoke();

            // Assert
            Assert.AreEqual(2, clickCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void SliderOnValueChanged_AsObservable_EmitsValues()
        {
            // Arrange
            var slider = _testGameObject.AddComponent<Slider>();
            var receivedValues = new List<float>();

            // Act
            var subscription = slider.onValueChanged.AsObservable()
                .Subscribe(value => receivedValues.Add(value));

            // Simulate slider value changes
            slider.onValueChanged.Invoke(0.5f);
            slider.onValueChanged.Invoke(0.8f);
            slider.onValueChanged.Invoke(0.2f);

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(0.5f, receivedValues[0], 0.001f);
            Assert.AreEqual(0.8f, receivedValues[1], 0.001f);
            Assert.AreEqual(0.2f, receivedValues[2], 0.001f);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void ToggleOnValueChanged_AsObservable_EmitsValues()
        {
            // Arrange
            var toggle = _testGameObject.AddComponent<Toggle>();
            var receivedValues = new List<bool>();

            // Act
            var subscription = toggle.onValueChanged.AsObservable()
                .Subscribe(value => receivedValues.Add(value));

            // Simulate toggle value changes
            toggle.onValueChanged.Invoke(true);
            toggle.onValueChanged.Invoke(false);
            toggle.onValueChanged.Invoke(true);

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { true, false, true }, receivedValues.ToArray());

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void InputFieldOnValueChanged_AsObservable_EmitsValues()
        {
            // Arrange
            var inputField = _testGameObject.AddComponent<InputField>();
            var receivedValues = new List<string>();

            // Act
            var subscription = inputField.onValueChanged.AsObservable()
                .Subscribe(value => receivedValues.Add(value));

            // Simulate input field value changes
            inputField.onValueChanged.Invoke("Hello");
            inputField.onValueChanged.Invoke("World");
            inputField.onValueChanged.Invoke("");

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { "Hello", "World", "" }, receivedValues.ToArray());

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void UnityEvent_MultipleSubscribers_AllReceiveEvents()
        {
            // Arrange
            var unityEvent = new UnityEvent<int>();
            var subscriber1Values = new List<int>();
            var subscriber2Values = new List<int>();

            // Act
            var subscription1 = unityEvent.AsObservable()
                .Subscribe(value => subscriber1Values.Add(value));
            var subscription2 = unityEvent.AsObservable()
                .Subscribe(value => subscriber2Values.Add(value));

            unityEvent.Invoke(42);

            // Assert
            Assert.AreEqual(1, subscriber1Values.Count);
            Assert.AreEqual(1, subscriber2Values.Count);
            Assert.AreEqual(42, subscriber1Values[0]);
            Assert.AreEqual(42, subscriber2Values[0]);

            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }

        [Test]
        public void UnityEvent_SubscriptionDisposal_StopsReceivingEvents()
        {
            // Arrange
            var unityEvent = new UnityEvent<string>();
            var receivedValues = new List<string>();

            var subscription = unityEvent.AsObservable()
                .Subscribe(value => receivedValues.Add(value));

            // Act
            unityEvent.Invoke("Before disposal");
            subscription.Dispose();
            unityEvent.Invoke("After disposal");

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual("Before disposal", receivedValues[0]);
        }

        [Test]
        public void UnityEvent_NullArgument_ThrowsException()
        {
            // Arrange
            UnityEvent nullEvent = null;
            UnityEvent<int> nullGenericEvent = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => nullEvent.AsObservable());
            Assert.Throws<ArgumentNullException>(() => nullGenericEvent.AsObservable());
        }

        [Test]
        public void UnityEvent_WithOperators_WorksCorrectly()
        {
            // Arrange
            var unityEvent = new UnityEvent<int>();
            var receivedValues = new List<int>();

            // Act
            var subscription = unityEvent.AsObservable()
                .Where(x => x > 10)
                .Select(x => x * 2)
                .Subscribe(value => receivedValues.Add(value));

            unityEvent.Invoke(5);   // Filtered out
            unityEvent.Invoke(15);  // 15 * 2 = 30
            unityEvent.Invoke(20);  // 20 * 2 = 40

            // Assert
            Assert.AreEqual(2, receivedValues.Count);
            Assert.AreEqual(new[] { 30, 40 }, receivedValues.ToArray());

            // Cleanup
            subscription.Dispose();
        }
    }
}
