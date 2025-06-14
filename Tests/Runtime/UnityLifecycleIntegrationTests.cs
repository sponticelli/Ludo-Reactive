using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Integration tests for Unity MonoBehaviour lifecycle integration with reactive system.
    /// </summary>
    public class UnityLifecycleIntegrationTests
    {
        private GameObject _testGameObject;
        private TestMonoBehaviour _testComponent;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestLifecycle");
            _testComponent = _testGameObject.AddComponent<TestMonoBehaviour>();
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
        public void AddTo_DisposesSubscriptionOnDestroy()
        {
            // Arrange
            var subject = new Subject<int>();
            var receivedValues = new List<int>();
            var disposed = false;

            var subscription = subject.Subscribe(value => receivedValues.Add(value));
            subscription.AddTo(_testComponent);

            // Track disposal
            var disposableWrapper = Disposable.Create(() => disposed = true);
            disposableWrapper.AddTo(_testComponent);

            // Act - Send value before destroy
            subject.OnNext(1);
            Assert.AreEqual(1, receivedValues.Count);

            // Destroy the GameObject
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            _testGameObject = null;

            // Send value after destroy
            subject.OnNext(2);

            // Assert
            Assert.IsTrue(disposed, "Disposable should be disposed when GameObject is destroyed");
            Assert.AreEqual(1, receivedValues.Count, "Should not receive values after disposal");

            // Cleanup
            subject.Dispose();
        }

        [UnityTest]
        public IEnumerator AddTo_DisposesOnApplicationPause()
        {
            // Arrange
            var subject = new Subject<int>();
            var receivedValues = new List<int>();
            var disposed = false;

            var subscription = subject.Subscribe(value => receivedValues.Add(value));
            subscription.AddTo(_testComponent);

            var disposableWrapper = Disposable.Create(() => disposed = true);
            disposableWrapper.AddTo(_testComponent);

            // Act - Send value before pause
            subject.OnNext(1);
            Assert.AreEqual(1, receivedValues.Count);

            // Simulate application pause
            _testComponent.SimulateApplicationPause(true);
            yield return null;

            // Send value after pause
            subject.OnNext(2);

            // Assert
            Assert.IsTrue(disposed, "Disposable should be disposed on application pause");
            Assert.AreEqual(1, receivedValues.Count, "Should not receive values after disposal");

            // Cleanup
            subject.Dispose();
        }

        [UnityTest]
        public IEnumerator AddTo_DisposesOnApplicationFocusLost()
        {
            // Arrange
            var subject = new Subject<int>();
            var receivedValues = new List<int>();
            var disposed = false;

            var subscription = subject.Subscribe(value => receivedValues.Add(value));
            subscription.AddTo(_testComponent);

            var disposableWrapper = Disposable.Create(() => disposed = true);
            disposableWrapper.AddTo(_testComponent);

            // Act - Send value before focus lost
            subject.OnNext(1);
            Assert.AreEqual(1, receivedValues.Count);

            // Simulate application focus lost
            _testComponent.SimulateApplicationFocus(false);
            yield return null;

            // Send value after focus lost
            subject.OnNext(2);

            // Assert
            Assert.IsTrue(disposed, "Disposable should be disposed on application focus lost");
            Assert.AreEqual(1, receivedValues.Count, "Should not receive values after disposal");

            // Cleanup
            subject.Dispose();
        }

        [Test]
        public void UpdateAsObservable_EmitsEveryFrame()
        {
            // Arrange
            var updateCount = 0;
            var subscription = _testComponent.UpdateAsObservable()
                .Subscribe(_ => updateCount++);

            // Act - Simulate multiple Update calls
            for (int i = 0; i < 5; i++)
            {
                _testComponent.SimulateUpdate();
            }

            // Assert
            Assert.AreEqual(5, updateCount, "Should emit once per Update call");

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void FixedUpdateAsObservable_EmitsEveryFixedFrame()
        {
            // Arrange
            var fixedUpdateCount = 0;
            var subscription = _testComponent.FixedUpdateAsObservable()
                .Subscribe(_ => fixedUpdateCount++);

            // Act - Simulate multiple FixedUpdate calls
            for (int i = 0; i < 3; i++)
            {
                _testComponent.SimulateFixedUpdate();
            }

            // Assert
            Assert.AreEqual(3, fixedUpdateCount, "Should emit once per FixedUpdate call");

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void LateUpdateAsObservable_EmitsEveryLateFrame()
        {
            // Arrange
            var lateUpdateCount = 0;
            var subscription = _testComponent.LateUpdateAsObservable()
                .Subscribe(_ => lateUpdateCount++);

            // Act - Simulate multiple LateUpdate calls
            for (int i = 0; i < 4; i++)
            {
                _testComponent.SimulateLateUpdate();
            }

            // Assert
            Assert.AreEqual(4, lateUpdateCount, "Should emit once per LateUpdate call");

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void OnDestroyAsObservable_EmitsOnDestroy()
        {
            // Arrange
            var destroyEmitted = false;
            var subscription = _testComponent.OnDestroyAsObservable()
                .Subscribe(_ => destroyEmitted = true);

            // Act
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            _testGameObject = null;

            // Assert
            Assert.IsTrue(destroyEmitted, "Should emit when GameObject is destroyed");

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void OnEnableDisableAsObservable_EmitsOnStateChange()
        {
            // Arrange - Create a fresh GameObject that starts inactive
            var testGO = new GameObject("OnEnableDisableTest");
            testGO.SetActive(false);
            var testComp = testGO.AddComponent<TestMonoBehaviour>();

            var enableCount = 0;
            var disableCount = 0;

            var enableSubscription = testComp.OnEnableAsObservable()
                .Subscribe(_ => enableCount++);
            var disableSubscription = testComp.OnDisableAsObservable()
                .Subscribe(_ => disableCount++);

            // Act - Enable the GameObject (should emit once)
            testGO.SetActive(true);

            // Then disable it (should emit once)
            testGO.SetActive(false);

            // Assert
            Assert.AreEqual(1, enableCount, "Should emit once on enable");
            Assert.AreEqual(1, disableCount, "Should emit once on disable");

            // Cleanup
            enableSubscription.Dispose();
            disableSubscription.Dispose();
            UnityEngine.Object.DestroyImmediate(testGO);
        }

        [Test]
        public void MultipleLifecycleObservables_WorkIndependently()
        {
            // Arrange
            var updateCount = 0;
            var fixedUpdateCount = 0;
            var lateUpdateCount = 0;

            var updateSub = _testComponent.UpdateAsObservable().Subscribe(_ => updateCount++);
            var fixedSub = _testComponent.FixedUpdateAsObservable().Subscribe(_ => fixedUpdateCount++);
            var lateSub = _testComponent.LateUpdateAsObservable().Subscribe(_ => lateUpdateCount++);

            // Act
            _testComponent.SimulateUpdate();
            _testComponent.SimulateFixedUpdate();
            _testComponent.SimulateLateUpdate();

            // Assert
            Assert.AreEqual(1, updateCount);
            Assert.AreEqual(1, fixedUpdateCount);
            Assert.AreEqual(1, lateUpdateCount);

            // Cleanup
            updateSub.Dispose();
            fixedSub.Dispose();
            lateSub.Dispose();
        }

        [Test]
        public void LifecycleObservables_HandleExceptionsGracefully()
        {
            // Arrange
            var goodObserverCalled = false;
            var updateSubscription = _testComponent.UpdateAsObservable()
                .Subscribe(_ => throw new Exception("Test exception"));

            var goodSubscription = _testComponent.UpdateAsObservable()
                .Subscribe(_ => goodObserverCalled = true);

            // Expect the error log message from the exception handling
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[Ludo\.Reactive\] Observer OnNext threw exception: System\.Exception: Test exception"));

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _testComponent.SimulateUpdate());
            Assert.IsTrue(goodObserverCalled, "Good observer should still be called despite exception in other observer");

            // Cleanup
            updateSubscription.Dispose();
            goodSubscription.Dispose();
        }

        /// <summary>
        /// Test MonoBehaviour for simulating Unity lifecycle events.
        /// </summary>
        private class TestMonoBehaviour : MonoBehaviour
        {
            public void SimulateUpdate() => SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            public void SimulateFixedUpdate() => SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            public void SimulateLateUpdate() => SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            public void SimulateOnEnable() => SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
            public void SimulateOnDisable() => SendMessage("OnDisable", SendMessageOptions.DontRequireReceiver);
            public void SimulateApplicationPause(bool pause) => SendMessage("OnApplicationPause", pause, SendMessageOptions.DontRequireReceiver);
            public void SimulateApplicationFocus(bool focus) => SendMessage("OnApplicationFocus", focus, SendMessageOptions.DontRequireReceiver);
        }
    }
}
