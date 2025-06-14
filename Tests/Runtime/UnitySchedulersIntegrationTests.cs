using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Integration tests for Unity schedulers functionality.
    /// </summary>
    public class UnitySchedulersIntegrationTests
    {
        [UnityTest]
        public IEnumerator MainThreadScheduler_ExecutesOnMainThread()
        {
            // Arrange
            var mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var executedThreadId = 0;
            var executed = false;

            // Act
            var subscription = UnitySchedulers.MainThread.Schedule(Unit.Default, (scheduler, state) =>
            {
                executedThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                executed = true;
                return Disposable.Empty;
            });

            yield return null; // Wait one frame

            // Assert
            Assert.IsTrue(executed);
            Assert.AreEqual(mainThreadId, executedThreadId);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator MainThreadScheduler_WithDelay_ExecutesAfterDelay()
        {
            // Arrange
            var startTime = Time.time;
            var executedTime = 0f;
            var executed = false;
            var delay = TimeSpan.FromSeconds(0.2f);

            // Act
            var subscription = UnitySchedulers.MainThread.Schedule(Unit.Default, delay, (scheduler, state) =>
            {
                executedTime = Time.time;
                executed = true;
                return Disposable.Empty;
            });

            yield return new WaitForSeconds(0.3f);

            // Assert
            Assert.IsTrue(executed);
            Assert.GreaterOrEqual(executedTime - startTime, 0.2f);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator FixedUpdateScheduler_ExecutesDuringFixedUpdate()
        {
            // Arrange
            var executionCount = 0;
            var executed = false;

            // Act
            var subscription = UnitySchedulers.FixedUpdate.Schedule(Unit.Default, (scheduler, state) =>
            {
                executionCount++;
                executed = true;
                return Disposable.Empty;
            });

            // Wait for a few fixed update cycles
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // Assert
            Assert.IsTrue(executed);
            Assert.GreaterOrEqual(executionCount, 1);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator LateUpdateScheduler_ExecutesDuringLateUpdate()
        {
            // Arrange
            var executed = false;

            // Act
            var subscription = UnitySchedulers.LateUpdate.Schedule(Unit.Default, (scheduler, state) =>
            {
                executed = true;
                return Disposable.Empty;
            });

            yield return null; // Wait one frame

            // Assert
            Assert.IsTrue(executed);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator EndOfFrameScheduler_ExecutesAtEndOfFrame()
        {
            // Arrange
            var executed = false;
            var frameCount = Time.frameCount;
            var executedFrameCount = 0;

            // Act
            var subscription = UnitySchedulers.EndOfFrame.Schedule(Unit.Default, (scheduler, state) =>
            {
                executed = true;
                executedFrameCount = Time.frameCount;
                return Disposable.Empty;
            });

            yield return new WaitForEndOfFrame();

            // Assert
            Assert.IsTrue(executed);
            Assert.GreaterOrEqual(executedFrameCount, frameCount);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator SchedulePeriodic_ExecutesRepeatedly()
        {
            // Arrange
            var executionCount = 0;
            var period = TimeSpan.FromSeconds(0.1f);

            // Act
            var subscription = UnitySchedulers.MainThread.SchedulePeriodic(Unit.Default, period, state =>
            {
                executionCount++;
                return state;
            });

            yield return new WaitForSeconds(0.35f);

            // Assert
            Assert.GreaterOrEqual(executionCount, 3, "Should execute at least 3 times in 0.35 seconds with 0.1s period");

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator SchedulePeriodic_CanBeCancelled()
        {
            // Arrange
            var executionCount = 0;
            var period = TimeSpan.FromSeconds(0.1f);

            // Act
            var subscription = UnitySchedulers.MainThread.SchedulePeriodic(Unit.Default, period, state =>
            {
                executionCount++;
                return state;
            });

            yield return new WaitForSeconds(0.15f);
            var countBeforeCancel = executionCount;
            subscription.Dispose();

            yield return new WaitForSeconds(0.2f);

            // Assert
            Assert.AreEqual(countBeforeCancel, executionCount, "Should not execute after cancellation");
        }

        [UnityTest]
        public IEnumerator ObserveOn_MainThread_ExecutesOnMainThread()
        {
            // Arrange
            var mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var observedThreadId = 0;
            var source = Observable.Return(42);

            // Act
            var subscription = source
                .ObserveOn(UnitySchedulers.MainThread)
                .Subscribe(value => observedThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId);

            yield return null;

            // Assert
            Assert.AreEqual(mainThreadId, observedThreadId);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator SubscribeOn_MainThread_SubscribesOnMainThread()
        {
            // Arrange
            var mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var subscribeThreadId = 0;
            var observeThreadId = 0;

            var source = Observable.Create<int>(observer =>
            {
                subscribeThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                observer.OnNext(42);
                observer.OnCompleted();
                return Disposable.Empty;
            });

            // Act
            var subscription = source
                .SubscribeOn(UnitySchedulers.MainThread)
                .Subscribe(value => observeThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId);

            yield return null;

            // Assert
            Assert.AreEqual(mainThreadId, subscribeThreadId);
            Assert.AreEqual(mainThreadId, observeThreadId);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator Delay_WithMainThreadScheduler_DelaysExecution()
        {
            // Arrange
            var startTime = Time.time;
            var receivedTime = 0f;
            var received = false;
            var delay = TimeSpan.FromSeconds(0.2f);

            // Act
            var subscription = Observable.Return(42)
                .Delay(delay, UnitySchedulers.MainThread)
                .Subscribe(value =>
                {
                    receivedTime = Time.time;
                    received = true;
                });

            yield return new WaitForSeconds(0.3f);

            // Assert
            Assert.IsTrue(received);
            Assert.GreaterOrEqual(receivedTime - startTime, 0.2f);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator Throttle_WithMainThreadScheduler_ThrottlesCorrectly()
        {
            // Arrange
            var subject = new Subject<int>();
            var receivedValues = new List<int>();
            var throttleDuration = TimeSpan.FromSeconds(0.1f);

            // Act
            var subscription = subject
                .Throttle(throttleDuration, UnitySchedulers.MainThread)
                .Subscribe(value => receivedValues.Add(value));

            subject.OnNext(1);
            subject.OnNext(2);
            subject.OnNext(3);

            yield return new WaitForSeconds(0.05f);
            subject.OnNext(4);

            yield return new WaitForSeconds(0.15f);

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual(4, receivedValues[0]);

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }

        [UnityTest]
        public IEnumerator Buffer_WithMainThreadScheduler_BuffersCorrectly()
        {
            // Arrange
            var subject = new Subject<int>();
            var receivedValues = new List<int>();
            var bufferPeriod = TimeSpan.FromSeconds(0.1f);

            // Act
            var subscription = subject
                .Buffer(bufferPeriod, UnitySchedulers.MainThread)
                .Subscribe(buffer => receivedValues.AddRange(buffer));

            subject.OnNext(1);
            yield return new WaitForSeconds(0.05f);
            subject.OnNext(2);
            yield return new WaitForSeconds(0.1f);
            subject.OnNext(3);
            yield return new WaitForSeconds(0.15f);

            // Assert
            Assert.GreaterOrEqual(receivedValues.Count, 2);
            Assert.Contains(1, receivedValues);
            Assert.Contains(2, receivedValues);

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }

        [Test]
        public void UnitySchedulers_AreSingletons()
        {
            // Act & Assert
            Assert.AreSame(UnitySchedulers.MainThread, UnitySchedulers.MainThread);
            Assert.AreSame(UnitySchedulers.FixedUpdate, UnitySchedulers.FixedUpdate);
            Assert.AreSame(UnitySchedulers.LateUpdate, UnitySchedulers.LateUpdate);
            Assert.AreSame(UnitySchedulers.EndOfFrame, UnitySchedulers.EndOfFrame);
        }

        [Test]
        public void UnitySchedulers_AreNotNull()
        {
            // Act & Assert
            Assert.IsNotNull(UnitySchedulers.MainThread);
            Assert.IsNotNull(UnitySchedulers.FixedUpdate);
            Assert.IsNotNull(UnitySchedulers.LateUpdate);
            Assert.IsNotNull(UnitySchedulers.EndOfFrame);
        }

        [UnityTest]
        public IEnumerator SchedulerException_DoesNotBreakScheduler()
        {
            // Arrange
            var goodActionExecuted = false;

            // Expect the error log from the exception (using regex to match the beginning)
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"^\[Ludo\.Reactive\] Scheduled action threw exception: System\.Exception: Test exception"));

            // Act - Schedule action that throws
            var badSubscription = UnitySchedulers.MainThread.Schedule(Unit.Default, (scheduler, state) =>
            {
                throw new Exception("Test exception");
            });

            yield return null;

            // Schedule good action after exception
            var goodSubscription = UnitySchedulers.MainThread.Schedule(Unit.Default, (scheduler, state) =>
            {
                goodActionExecuted = true;
                return Disposable.Empty;
            });

            yield return null;

            // Assert
            Assert.IsTrue(goodActionExecuted, "Scheduler should continue working after exception");

            // Cleanup
            badSubscription.Dispose();
            goodSubscription.Dispose();
        }

        [UnityTest]
        public IEnumerator MultipleSchedulers_WorkIndependently()
        {
            // Arrange
            var mainThreadExecuted = false;
            var lateUpdateExecuted = false;
            var endOfFrameExecuted = false;

            // Act
            var mainSub = UnitySchedulers.MainThread.Schedule(Unit.Default, (scheduler, state) =>
            {
                mainThreadExecuted = true;
                return Disposable.Empty;
            });
            var lateSub = UnitySchedulers.LateUpdate.Schedule(Unit.Default, (scheduler, state) =>
            {
                lateUpdateExecuted = true;
                return Disposable.Empty;
            });
            var endSub = UnitySchedulers.EndOfFrame.Schedule(Unit.Default, (scheduler, state) =>
            {
                endOfFrameExecuted = true;
                return Disposable.Empty;
            });

            // Wait for Update and LateUpdate to execute
            yield return null;

            // Wait for EndOfFrame to execute
            yield return new WaitForEndOfFrame();

            // Assert
            Assert.IsTrue(mainThreadExecuted, "MainThread scheduler should have executed");
            Assert.IsTrue(lateUpdateExecuted, "LateUpdate scheduler should have executed");
            Assert.IsTrue(endOfFrameExecuted, "EndOfFrame scheduler should have executed");

            // Cleanup
            mainSub.Dispose();
            lateSub.Dispose();
            endSub.Dispose();
        }
    }
}
