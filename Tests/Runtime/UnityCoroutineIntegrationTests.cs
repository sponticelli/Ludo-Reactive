using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Integration tests for Unity coroutine-based operations with reactive system.
    /// </summary>
    public class UnityCoroutineIntegrationTests
    {
        private GameObject _testGameObject;
        private TestCoroutineRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestCoroutine");
            _runner = _testGameObject.AddComponent<TestCoroutineRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        [UnityTest]
        public IEnumerator FromCoroutine_EmitsValuesFromCoroutine()
        {
            // Arrange
            var receivedValues = new List<int>();
            var completed = false;

            // Act
            var subscription = Observable.FromCoroutine<int>(observer => _runner.TestCoroutine(observer))
                .Subscribe(
                    value => receivedValues.Add(value),
                    onCompleted: () => completed = true
                );

            // Wait for coroutine to complete
            yield return new WaitForSeconds(0.5f);

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 2, 3 }, receivedValues.ToArray());
            Assert.IsTrue(completed);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator FromCoroutine_HandlesCoroutineException()
        {
            // Arrange
            var receivedValues = new List<int>();
            var receivedException = default(Exception);
            var completed = false;

            // Act
            var subscription = Observable.FromCoroutine<int>(observer => _runner.ExceptionCoroutine(observer))
                .Subscribe(
                    value => receivedValues.Add(value),
                    error => receivedException = error,
                    onCompleted: () => completed = true
                );

            // Wait for coroutine to complete
            yield return new WaitForSeconds(0.3f);

            // Assert
            Assert.AreEqual(1, receivedValues.Count); // Should receive one value before exception
            Assert.AreEqual(1, receivedValues[0]);
            Assert.IsNotNull(receivedException);
            Assert.AreEqual("Test coroutine exception", receivedException.Message);
            Assert.IsFalse(completed);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator FromCoroutine_CanBeCancelled()
        {
            // Arrange
            var receivedValues = new List<int>();
            var completed = false;

            // Act
            var subscription = Observable.FromCoroutine<int>(observer => _runner.LongRunningCoroutine(observer))
                .Subscribe(
                    value => receivedValues.Add(value),
                    onCompleted: () => completed = true
                );

            // Wait a bit, then cancel
            yield return new WaitForSeconds(0.2f);
            subscription.Dispose();

            // Wait more to see if it continues
            yield return new WaitForSeconds(0.5f);

            // Assert
            Assert.Less(receivedValues.Count, 5, "Should not complete all values after cancellation");
            Assert.IsFalse(completed, "Should not complete after cancellation");
        }

        [UnityTest]
        public IEnumerator ToYieldInstruction_ConvertsObservableToCoroutine()
        {
            // Arrange
            var source = Observable.Range(1, 3).Delay(TimeSpan.FromSeconds(0.1f));
            var receivedValue = 0;

            // Act
            yield return source.ToYieldInstruction(value => receivedValue = value);

            // Assert
            Assert.AreEqual(3, receivedValue, "Should receive the last emitted value");
        }

        [UnityTest]
        public IEnumerator ToYieldInstruction_HandlesObservableError()
        {
            // Arrange
            var source = Observable.Throw<int>(new InvalidOperationException("Test error"));
            var receivedException = default(Exception);

            // Act
            yield return source.ToYieldInstruction(
                value => { },
                error => receivedException = error
            );

            // Assert
            Assert.IsNotNull(receivedException);
            Assert.AreEqual("Test error", receivedException.Message);
        }

        [UnityTest]
        public IEnumerator StartAsCoroutine_ExecutesObservableAsCoroutine()
        {
            // Arrange
            var receivedValues = new List<int>();
            var completed = false;

            var observable = Observable.Create<int>(observer =>
            {
                _runner.StartCoroutine(EmitValuesCoroutine(observer));
                return Disposable.Empty;
            });

            // Act
            var subscription = observable.Subscribe(
                value => receivedValues.Add(value),
                onCompleted: () => completed = true
            );

            // Wait for completion
            yield return new WaitForSeconds(0.4f);

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { 10, 20, 30 }, receivedValues.ToArray());
            Assert.IsTrue(completed);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator ObserveOnMainThread_ExecutesOnMainThread()
        {
            // Arrange
            var mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var observedThreadId = 0;
            var source = Observable.Return(42);

            // Act
            var subscription = source
                .ObserveOnMainThread()
                .Subscribe(value => observedThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId);

            yield return null; // Wait one frame

            // Assert
            Assert.AreEqual(mainThreadId, observedThreadId, "Should observe on main thread");

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator DelayFrame_DelaysExecutionByFrames()
        {
            // Arrange
            var frameCount = Time.frameCount;
            var observedFrameCount = 0;

            // Act
            var subscription = Observable.Return(Unit.Default)
                .DelayFrame(3)
                .Subscribe(_ => observedFrameCount = Time.frameCount);

            // Wait for delay
            yield return null;
            yield return null;
            yield return null;
            yield return null; // One extra frame to ensure execution

            // Assert
            Assert.GreaterOrEqual(observedFrameCount - frameCount, 3, "Should delay by at least 3 frames");

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator TakeUntilDestroy_StopsOnGameObjectDestroy()
        {
            // Arrange
            var receivedValues = new List<int>();
            var completed = false;
            var subject = new Subject<int>();

            // Create a simple trigger observable to test TakeUntil behavior
            var triggerSubject = new Subject<Unit>();

            // Test TakeUntil with manual trigger first
            var manualSubscription = subject
                .TakeUntil(triggerSubject)
                .Subscribe(
                    value => receivedValues.Add(value),
                    onCompleted: () => completed = true
                );

            // Send some values
            subject.OnNext(1);
            subject.OnNext(2);

            // Trigger completion
            triggerSubject.OnNext(Unit.Default);

            // Try to send more values (these should be ignored)
            subject.OnNext(3);
            subject.OnNext(4);

            // Assert manual TakeUntil works
            Assert.AreEqual(2, receivedValues.Count, $"Manual TakeUntil failed. Expected 2 values but got {receivedValues.Count}: [{string.Join(", ", receivedValues)}]");
            Assert.IsTrue(completed, "Manual TakeUntil should complete");

            // Cleanup manual test
            manualSubscription.Dispose();
            triggerSubject.Dispose();

            // Reset for GameObject test
            receivedValues.Clear();
            completed = false;

            // Now test with GameObject destruction
            var destroyObservable = _testGameObject.OnDestroyAsObservable();
            var destroyEmitted = false;
            var destroyCompleted = false;

            // Subscribe to destroy observable to verify it works
            var destroySubscription = destroyObservable.Subscribe(
                _ => destroyEmitted = true,
                onCompleted: () => destroyCompleted = true
            );

            // Act
            var subscription = subject
                .TakeUntilDestroy(_testGameObject)
                .Subscribe(
                    value => receivedValues.Add(value),
                    onCompleted: () => completed = true
                );

            // Send some values
            subject.OnNext(5);
            subject.OnNext(6);
            yield return null;

            // Destroy the GameObject
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            _testGameObject = null;
            yield return null; // Wait for destruction to be processed
            yield return null; // Extra frame to ensure OnDestroy is called

            // Verify destroy observable emitted
            Assert.IsTrue(destroyEmitted, "Destroy observable should have emitted");
            Assert.IsTrue(destroyCompleted, "Destroy observable should have completed");

            // Try to send more values (these should be ignored)
            subject.OnNext(7);
            subject.OnNext(8);
            yield return null; // Wait for any potential processing

            // Assert
            Assert.AreEqual(2, receivedValues.Count, $"Expected 2 values but got {receivedValues.Count}: [{string.Join(", ", receivedValues)}]");
            Assert.AreEqual(new[] { 5, 6 }, receivedValues.ToArray());
            Assert.IsTrue(completed, "Should complete when GameObject is destroyed");

            // Cleanup
            subscription.Dispose();
            destroySubscription.Dispose();
            subject.Dispose();
        }

        private IEnumerator EmitValuesCoroutine(IObserver<int> observer)
        {
            observer.OnNext(10);
            yield return new WaitForSeconds(0.1f);
            observer.OnNext(20);
            yield return new WaitForSeconds(0.1f);
            observer.OnNext(30);
            observer.OnCompleted();
        }

        /// <summary>
        /// Test MonoBehaviour for running coroutines in tests.
        /// </summary>
        private class TestCoroutineRunner : MonoBehaviour
        {
            public IEnumerator TestCoroutine(IObserver<int> observer)
            {
                observer.OnNext(1);
                yield return new WaitForSeconds(0.1f);
                observer.OnNext(2);
                yield return new WaitForSeconds(0.1f);
                observer.OnNext(3);
                observer.OnCompleted();
            }

            public IEnumerator ExceptionCoroutine(IObserver<int> observer)
            {
                observer.OnNext(1);
                yield return new WaitForSeconds(0.1f);
                observer.OnError(new Exception("Test coroutine exception"));
            }

            public IEnumerator LongRunningCoroutine(IObserver<int> observer)
            {
                for (int i = 1; i <= 10; i++)
                {
                    observer.OnNext(i);
                    yield return new WaitForSeconds(0.1f);
                }
                observer.OnCompleted();
            }
        }
    }
}
