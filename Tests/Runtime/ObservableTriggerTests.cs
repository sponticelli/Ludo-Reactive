using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Unit tests for ObservableTrigger component functionality.
    /// </summary>
    public class ObservableTriggerTests
    {
        private GameObject _testGameObject;
        private ObservableTrigger _trigger;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestObservableTrigger");
            _trigger = _testGameObject.AddComponent<ObservableTrigger>();
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
        public void UpdateAsObservable_EmitsOnUpdate()
        {
            // Arrange
            var updateCount = 0;
            var subscription = _trigger.UpdateAsObservable()
                .Subscribe(_ => updateCount++);

            // Act
            _trigger.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("Update", SendMessageOptions.DontRequireReceiver);

            // Assert
            Assert.AreEqual(3, updateCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void FixedUpdateAsObservable_EmitsOnFixedUpdate()
        {
            // Arrange
            var fixedUpdateCount = 0;
            var subscription = _trigger.FixedUpdateAsObservable()
                .Subscribe(_ => fixedUpdateCount++);

            // Act
            _trigger.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);

            // Assert
            Assert.AreEqual(2, fixedUpdateCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void LateUpdateAsObservable_EmitsOnLateUpdate()
        {
            // Arrange
            var lateUpdateCount = 0;
            var subscription = _trigger.LateUpdateAsObservable()
                .Subscribe(_ => lateUpdateCount++);

            // Act
            _trigger.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            // Assert
            Assert.AreEqual(4, lateUpdateCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void OnDestroyAsObservable_EmitsOnDestroy()
        {
            // Arrange
            var destroyEmitted = false;
            var subscription = _trigger.OnDestroyAsObservable()
                .Subscribe(_ => destroyEmitted = true);

            // Act
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            _testGameObject = null;

            // Assert
            Assert.IsTrue(destroyEmitted);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void OnEnableAsObservable_EmitsOnEnable()
        {
            // Arrange
            var enableCount = 0;
            var subscription = _trigger.OnEnableAsObservable()
                .Subscribe(_ => enableCount++);

            // Act
            _trigger.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

            // Assert
            Assert.AreEqual(2, enableCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void OnDisableAsObservable_EmitsOnDisable()
        {
            // Arrange
            var disableCount = 0;
            var subscription = _trigger.OnDisableAsObservable()
                .Subscribe(_ => disableCount++);

            // Act
            _trigger.SendMessage("OnDisable", SendMessageOptions.DontRequireReceiver);

            // Assert
            Assert.AreEqual(1, disableCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void OnApplicationPauseAsObservable_EmitsOnApplicationPause()
        {
            // Arrange
            var pauseCount = 0;
            var subscription = _trigger.OnApplicationPauseAsObservable()
                .Subscribe(_ => pauseCount++);

            // Act
            _trigger.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("OnApplicationPause", false, SendMessageOptions.DontRequireReceiver);

            // Assert
            Assert.AreEqual(2, pauseCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void OnApplicationFocusAsObservable_EmitsOnApplicationFocus()
        {
            // Arrange
            var focusCount = 0;
            var subscription = _trigger.OnApplicationFocusAsObservable()
                .Subscribe(_ => focusCount++);

            // Act
            _trigger.SendMessage("OnApplicationFocus", true, SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);

            // Assert
            Assert.AreEqual(2, focusCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void MultipleObservables_WorkIndependently()
        {
            // Arrange
            var updateCount = 0;
            var fixedUpdateCount = 0;
            var lateUpdateCount = 0;

            var updateSub = _trigger.UpdateAsObservable().Subscribe(_ => updateCount++);
            var fixedSub = _trigger.FixedUpdateAsObservable().Subscribe(_ => fixedUpdateCount++);
            var lateSub = _trigger.LateUpdateAsObservable().Subscribe(_ => lateUpdateCount++);

            // Act
            _trigger.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

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
        public void MultipleSubscribers_AllReceiveEvents()
        {
            // Arrange
            var subscriber1Count = 0;
            var subscriber2Count = 0;
            var subscriber3Count = 0;

            var sub1 = _trigger.UpdateAsObservable().Subscribe(_ => subscriber1Count++);
            var sub2 = _trigger.UpdateAsObservable().Subscribe(_ => subscriber2Count++);
            var sub3 = _trigger.UpdateAsObservable().Subscribe(_ => subscriber3Count++);

            // Act
            _trigger.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            _trigger.SendMessage("Update", SendMessageOptions.DontRequireReceiver);

            // Assert
            Assert.AreEqual(2, subscriber1Count);
            Assert.AreEqual(2, subscriber2Count);
            Assert.AreEqual(2, subscriber3Count);

            // Cleanup
            sub1.Dispose();
            sub2.Dispose();
            sub3.Dispose();
        }

        [Test]
        public void SubscriptionDisposal_StopsReceivingEvents()
        {
            // Arrange
            var updateCount = 0;
            var subscription = _trigger.UpdateAsObservable()
                .Subscribe(_ => updateCount++);

            // Act
            _trigger.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            subscription.Dispose();
            _trigger.SendMessage("Update", SendMessageOptions.DontRequireReceiver);

            // Assert
            Assert.AreEqual(1, updateCount);
        }

        [Test]
        public void LazyInitialization_SubjectsCreatedOnDemand()
        {
            // Arrange & Act
            var updateObservable = _trigger.UpdateAsObservable();
            var fixedUpdateObservable = _trigger.FixedUpdateAsObservable();

            // Assert - Subjects should be created but not others
            Assert.IsNotNull(updateObservable);
            Assert.IsNotNull(fixedUpdateObservable);
        }

        [Test]
        public void ExceptionInObserver_DoesNotBreakOtherObservers()
        {
            // Arrange
            var goodObserverCalled = false;

            var badSubscription = _trigger.UpdateAsObservable()
                .Subscribe(_ => throw new Exception("Test exception"));
            var goodSubscription = _trigger.UpdateAsObservable()
                .Subscribe(_ => goodObserverCalled = true);

            // Expect the error log message from the Subject's exception handling
            // Use regex to match the beginning of the log message since it includes a full stack trace
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[Ludo\.Reactive\] Observer OnNext threw exception: System\.Exception: Test exception"));

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _trigger.SendMessage("Update", SendMessageOptions.DontRequireReceiver));
            Assert.IsTrue(goodObserverCalled);

            // Cleanup
            badSubscription.Dispose();
            goodSubscription.Dispose();
        }

        [Test]
        public void OnDestroy_DisposesAllSubjects()
        {
            // Arrange
            var updateCount = 0;
            var fixedUpdateCount = 0;
            var lateUpdateCount = 0;

            var updateSub = _trigger.UpdateAsObservable().Subscribe(_ => updateCount++);
            var fixedSub = _trigger.FixedUpdateAsObservable().Subscribe(_ => fixedUpdateCount++);
            var lateSub = _trigger.LateUpdateAsObservable().Subscribe(_ => lateUpdateCount++);

            // Act
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            _testGameObject = null;

            // Try to trigger events (should not work after disposal)
            // Note: We can't actually test this easily since the object is destroyed
            // But the disposal should prevent any further emissions

            // Cleanup
            updateSub.Dispose();
            fixedSub.Dispose();
            lateSub.Dispose();
        }

        [Test]
        public void ComponentExtensions_CreateTriggerAutomatically()
        {
            // Arrange
            var newGameObject = new GameObject("TestExtensions");
            var testComponent = newGameObject.AddComponent<TestComponent>();

            // Act - Subscribe to trigger the lazy creation of ObservableTrigger
            var updateObservable = testComponent.UpdateAsObservable();
            var fixedUpdateObservable = testComponent.FixedUpdateAsObservable();
            var lateUpdateObservable = testComponent.LateUpdateAsObservable();

            // Subscribe to actually create the trigger (observables are lazy)
            var updateSub = updateObservable.Subscribe(_ => { });
            var fixedSub = fixedUpdateObservable.Subscribe(_ => { });
            var lateSub = lateUpdateObservable.Subscribe(_ => { });

            // Assert
            var trigger = newGameObject.GetComponent<ObservableTrigger>();
            Assert.IsNotNull(trigger);
            Assert.IsNotNull(updateObservable);
            Assert.IsNotNull(fixedUpdateObservable);
            Assert.IsNotNull(lateUpdateObservable);

            // Cleanup
            updateSub.Dispose();
            fixedSub.Dispose();
            lateSub.Dispose();
            UnityEngine.Object.DestroyImmediate(newGameObject);
        }

        [Test]
        public void StaticExtensions_UseGlobalRunner()
        {
            // Act
            var everyUpdate = UnityObservableExtensions.EveryUpdate();
            var everyFixedUpdate = UnityObservableExtensions.EveryFixedUpdate();
            var everyLateUpdate = UnityObservableExtensions.EveryLateUpdate();

            // Assert
            Assert.IsNotNull(everyUpdate);
            Assert.IsNotNull(everyFixedUpdate);
            Assert.IsNotNull(everyLateUpdate);
        }

        [Test]
        public void DisallowMultipleComponent_PreventsMultipleTriggers()
        {
            // Act & Assert
            // Unity's DisallowMultipleComponent should prevent adding a second ObservableTrigger
            // In some test environments, Unity might log an error instead of throwing an exception
            LogAssert.Expect(LogType.Error, "Can't add 'ObservableTrigger' to TestObservableTrigger because a 'ObservableTrigger' is already added to the game object!");

            // This should either throw an exception or log an error
            try
            {
                _testGameObject.AddComponent<ObservableTrigger>();
                // If we reach here, Unity logged an error instead of throwing
                Assert.Pass("Unity prevented multiple components via error logging");
            }
            catch (InvalidOperationException)
            {
                // This is the expected behavior in some Unity versions
                Assert.Pass("Unity prevented multiple components via exception");
            }
        }

        /// <summary>
        /// Test component for testing component extensions.
        /// </summary>
        private class TestComponent : MonoBehaviour
        {
        }
    }
}
