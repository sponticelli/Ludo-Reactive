using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Performance tests to verify the efficiency of the reactive system.
    /// </summary>
    public class PerformanceTests
    {
        [Test]
        public void ReactiveProperty_ManySubscribers_PerformsWell()
        {
            // Arrange
            const int subscriberCount = 1000;
            var property = new ReactiveProperty<int>(0);
            var subscriptions = new List<IDisposable>();
            var notificationCounts = new int[subscriberCount];

            // Subscribe many observers
            for (int i = 0; i < subscriberCount; i++)
            {
                var index = i; // Capture for closure
                var subscription = property.Subscribe(value => notificationCounts[index]++);
                subscriptions.Add(subscription);
            }

            var stopwatch = Stopwatch.StartNew();

            // Act - Change value multiple times
            for (int i = 1; i <= 100; i++)
            {
                property.Value = i;
            }

            stopwatch.Stop();

            // Assert
            UnityEngine.Debug.Log($"Time for 100 notifications to 1000 subscribers: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Should complete within 100ms");

            // Verify all subscribers received all notifications
            for (int i = 0; i < subscriberCount; i++)
            {
                Assert.AreEqual(101, notificationCounts[i], $"Subscriber {i} should receive initial + 100 notifications");
            }

            // Cleanup
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
            property.Dispose();
        }

        [Test]
        public void Observable_LongChain_PerformsWell()
        {
            // Arrange
            const int chainLength = 100;
            const int valueCount = 1000;
            var source = new Subject<int>();
            var receivedValues = new List<int>();

            // Build a long operator chain
            IObservable<int> observable = source;
            for (int i = 0; i < chainLength; i++)
            {
                observable = observable.Select(x => x + 1);
            }

            var subscription = observable.Subscribe(value => receivedValues.Add(value));

            var stopwatch = Stopwatch.StartNew();

            // Act - Send many values through the chain
            for (int i = 0; i < valueCount; i++)
            {
                source.OnNext(i);
            }

            stopwatch.Stop();

            // Assert
            UnityEngine.Debug.Log($"Time for {valueCount} values through {chainLength}-operator chain: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 500, "Should complete within 500ms");
            Assert.AreEqual(valueCount, receivedValues.Count);

            // Verify transformation is correct (each value should be incremented by chainLength)
            for (int i = 0; i < valueCount; i++)
            {
                Assert.AreEqual(i + chainLength, receivedValues[i]);
            }

            // Cleanup
            subscription.Dispose();
            source.Dispose();
        }

        [Test]
        public void DistinctUntilChanged_ManyDuplicates_PerformsWell()
        {
            // Arrange
            const int valueCount = 10000;
            var source = new Subject<int>();
            var receivedValues = new List<int>();

            var subscription = source
                .DistinctUntilChanged()
                .Subscribe(value => receivedValues.Add(value));

            var stopwatch = Stopwatch.StartNew();

            // Act - Send many duplicate values
            for (int i = 0; i < valueCount; i++)
            {
                source.OnNext(i / 100); // Creates many duplicates
            }

            stopwatch.Stop();

            // Assert
            UnityEngine.Debug.Log($"Time for {valueCount} values with DistinctUntilChanged: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Should complete within 100ms");
            Assert.AreEqual(100, receivedValues.Count, "Should only receive 100 distinct values");

            // Cleanup
            subscription.Dispose();
            source.Dispose();
        }

        [Test]
        public void CompositeDisposable_ManyDisposables_PerformsWell()
        {
            // Arrange
            const int disposableCount = 10000;
            var composite = new CompositeDisposable();
            var disposedCounts = new int[disposableCount];

            // Add many disposables
            for (int i = 0; i < disposableCount; i++)
            {
                var index = i; // Capture for closure
                var disposable = Disposable.Create(() => disposedCounts[index]++);
                composite.Add(disposable);
            }

            var stopwatch = Stopwatch.StartNew();

            // Act - Dispose all at once
            composite.Dispose();

            stopwatch.Stop();

            // Assert
            UnityEngine.Debug.Log($"Time to dispose {disposableCount} disposables: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 50, "Should complete within 50ms");

            // Verify all disposables were disposed
            for (int i = 0; i < disposableCount; i++)
            {
                Assert.AreEqual(1, disposedCounts[i], $"Disposable {i} should be disposed exactly once");
            }
        }

        [Test]
        public void Subject_ManyObserversAddRemove_PerformsWell()
        {
            // Arrange
            const int operationCount = 1000;
            var subject = new Subject<int>();
            var subscriptions = new List<IDisposable>();

            var stopwatch = Stopwatch.StartNew();

            // Act - Add and remove many observers
            for (int i = 0; i < operationCount; i++)
            {
                var subscription = subject.Subscribe(_ => { });
                subscriptions.Add(subscription);

                if (i % 2 == 0 && subscriptions.Count > 1)
                {
                    subscriptions[0].Dispose();
                    subscriptions.RemoveAt(0);
                }
            }

            stopwatch.Stop();

            // Assert
            UnityEngine.Debug.Log($"Time for {operationCount} subscribe/unsubscribe operations: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Should complete within 100ms");

            // Cleanup
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
            subject.Dispose();
        }

        [Test]
        public void ReactiveProperty_NoAllocation_ForSameValue()
        {
            // Arrange
            var property = new ReactiveProperty<int>(42);
            var notificationCount = 0;
            property.Subscribe(_ => notificationCount++);

            // Act - Set same value multiple times
            var initialGC = GC.CollectionCount(0);
            for (int i = 0; i < 1000; i++)
            {
                property.Value = 42; // Same value
            }
            var finalGC = GC.CollectionCount(0);

            // Assert
            Assert.AreEqual(initialGC, finalGC, "Should not trigger garbage collection");
            Assert.AreEqual(1, notificationCount, "Should only notify once (initial subscription)");

            // Cleanup
            property.Dispose();
        }

        [Test]
        public void Observable_Where_EarlyFiltering_PerformsWell()
        {
            // Arrange
            const int valueCount = 10000;
            var source = new Subject<int>();
            var receivedValues = new List<int>();

            // Chain with early filtering
            var subscription = source
                .Where(x => x % 1000 == 0) // Filter early - only 1% pass through
                .Select(x => x * 2)        // Expensive operation on filtered data
                .Select(x => x.ToString()) // Another operation
                .Subscribe(value => receivedValues.Add(int.Parse(value)));

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < valueCount; i++)
            {
                source.OnNext(i);
            }

            stopwatch.Stop();

            // Assert
            UnityEngine.Debug.Log($"Time for {valueCount} values with early filtering: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 50, "Should complete within 50ms due to early filtering");
            Assert.AreEqual(10, receivedValues.Count, "Should only receive filtered values");

            // Cleanup
            subscription.Dispose();
            source.Dispose();
        }

        [Test]
        public void BehaviorSubject_LateSubscribers_GetCurrentValue()
        {
            // Arrange
            var subject = new BehaviorSubject<string>("initial");
            var receivedValues = new List<string>();

            // Act - Change value before subscribing
            subject.OnNext("changed");
            
            var subscription = subject.Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual("changed", receivedValues[0], "Late subscriber should get current value");

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }

        [Test]
        public void ReplaySubject_BuffersCorrectly()
        {
            // Arrange
            const int bufferSize = 3;
            var subject = new ReplaySubject<int>(bufferSize);
            var receivedValues = new List<int>();

            // Act - Send more values than buffer size
            for (int i = 1; i <= 5; i++)
            {
                subject.OnNext(i);
            }

            var subscription = subject.Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(bufferSize, receivedValues.Count);
            Assert.AreEqual(new[] { 3, 4, 5 }, receivedValues.ToArray(), "Should only replay last 3 values");

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }
    }
}
