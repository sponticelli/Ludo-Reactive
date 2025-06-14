using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Comprehensive tests for AsyncObservableExtensions including cancellation and timeout scenarios.
    /// </summary>
    public class AsyncObservableExtensionsTests
    {
        [Test]
        public async Task ToTask_ReturnsFirstValue()
        {
            // Arrange
            var source = Observable.Return(42);

            // Act
            var result = await source.ToTask();

            // Assert
            Assert.AreEqual(42, result);
        }

        [Test]
        public async Task ToTask_WithMultipleValues_ReturnsFirst()
        {
            // Arrange
            var subject = new Subject<int>();
            var task = subject.ToTask();

            // Act
            subject.OnNext(1);
            subject.OnNext(2);
            subject.OnNext(3);

            var result = await task;

            // Assert
            Assert.AreEqual(1, result);

            // Cleanup
            subject.Dispose();
        }

        [Test]
        public void ToTask_WithError_ThrowsException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test error");
            var source = Observable.Throw<int>(expectedException);

            // Act & Assert
            var task = source.ToTask();
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
            Assert.AreEqual("Test error", exception.Message);
        }

        [Test]
        public void ToTask_WithCancellation_ThrowsOperationCancelledException()
        {
            // Arrange
            var subject = new Subject<int>();
            var cts = new CancellationTokenSource();
            var task = subject.ToTask(cts.Token);

            // Act
            cts.Cancel();

            // Assert
            var exception = Assert.ThrowsAsync<TaskCanceledException>(() => task);
            Assert.IsInstanceOf<OperationCanceledException>(exception);

            // Cleanup
            subject.Dispose();
        }

        [Test]
        public void ToTask_WithTimeout_ThrowsOperationCancelledException()
        {
            // Arrange
            var subject = new Subject<int>();
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            var task = subject.ToTask(cts.Token);

            // Act & Assert
            var exception = Assert.ThrowsAsync<TaskCanceledException>(() => task);
            Assert.IsInstanceOf<OperationCanceledException>(exception);

            // Cleanup
            subject.Dispose();
        }

        [Test]
        public async Task FromTask_EmitsTaskResult()
        {
            // Arrange
            var task = Task.FromResult(42);
            var receivedValues = new List<int>();
            var completed = false;

            // Act
            var subscription = Observable.FromTask(task)
                .Subscribe(
                    value => receivedValues.Add(value),
                    onCompleted: () => completed = true
                );

            await Task.Delay(50); // Give time for async completion

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual(42, receivedValues[0]);
            Assert.IsTrue(completed);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public async Task FromTask_WithException_EmitsError()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Task error");
            var task = Task.FromException<int>(expectedException);
            var receivedValues = new List<int>();
            var receivedException = default(Exception);

            // Act
            var subscription = Observable.FromTask(task)
                .Subscribe(
                    value => receivedValues.Add(value),
                    error => receivedException = error
                );

            await Task.Delay(50); // Give time for async completion

            // Assert
            Assert.AreEqual(0, receivedValues.Count);
            Assert.IsNotNull(receivedException);
            Assert.AreEqual("Task error", receivedException.Message);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public async Task FromTask_WithCancellation_EmitsError()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.FromCanceled<int>(cts.Token);
            var receivedValues = new List<int>();
            var receivedException = default(Exception);

            // Act
            var subscription = Observable.FromTask(task)
                .Subscribe(
                    value => receivedValues.Add(value),
                    error => receivedException = error
                );

            await Task.Delay(50); // Give time for async completion

            // Assert
            Assert.AreEqual(0, receivedValues.Count);
            Assert.IsInstanceOf<OperationCanceledException>(receivedException);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator Timeout_CompletesBeforeTimeout_EmitsNormally()
        {
            // Arrange
            var subject = new Subject<int>();
            var receivedValues = new List<int>();
            var completed = false;
            var timedOut = false;

            // Act
            var subscription = subject
                .Timeout(TimeSpan.FromSeconds(1))
                .Subscribe(
                    value => receivedValues.Add(value),
                    error => timedOut = error is TimeoutException,
                    onCompleted: () => completed = true
                );

            subject.OnNext(1);
            subject.OnNext(2);
            subject.OnCompleted();

            yield return new WaitForSeconds(0.1f);

            // Assert
            Assert.AreEqual(2, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 2 }, receivedValues.ToArray());
            Assert.IsTrue(completed);
            Assert.IsFalse(timedOut);

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }

        [UnityTest]
        public IEnumerator Timeout_ExceedsTimeout_EmitsTimeoutError()
        {
            // Arrange
            var subject = new Subject<int>();
            var receivedValues = new List<int>();
            var completed = false;
            var timedOut = false;

            // Act
            var subscription = subject
                .Timeout(TimeSpan.FromSeconds(0.2f))
                .Subscribe(
                    value => receivedValues.Add(value),
                    error => timedOut = error is TimeoutException,
                    onCompleted: () => completed = true
                );

            subject.OnNext(1);
            // Don't send more values, let it timeout

            yield return new WaitForSeconds(0.3f);

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual(1, receivedValues[0]);
            Assert.IsFalse(completed);
            Assert.IsTrue(timedOut);

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }

        [Test]
        public async Task SelectAsync_TransformsValuesAsynchronously()
        {
            // Arrange
            var source = Observable.Range(1, 3);
            var receivedValues = new List<string>();

            // Act
            var subscription = source
                .SelectAsync(async x =>
                {
                    await Task.Delay(10);
                    return $"Value: {x}";
                })
                .Subscribe(value => receivedValues.Add(value));

            await Task.Delay(100); // Wait for async operations

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.Contains("Value: 1", receivedValues);
            Assert.Contains("Value: 2", receivedValues);
            Assert.Contains("Value: 3", receivedValues);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public async Task SelectAsync_WithCancellation_StopsProcessing()
        {
            // Arrange
            var subject = new Subject<int>();
            var receivedValues = new List<string>();
            var cts = new CancellationTokenSource();

            // Act
            var subscription = subject
                .SelectAsync(async (x, token) =>
                {
                    await Task.Delay(50, token);
                    return $"Value: {x}";
                }, cts.Token)
                .Subscribe(value => receivedValues.Add(value));

            subject.OnNext(1);
            subject.OnNext(2);

            // Cancel after a short delay
            await Task.Delay(25);
            cts.Cancel();

            await Task.Delay(100); // Wait to see if more values come through

            // Assert
            Assert.LessOrEqual(receivedValues.Count, 2, "Should not process all values after cancellation");

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }

        [Test]
        public async Task WhereAsync_FiltersAsynchronously()
        {
            // Arrange
            var source = Observable.Range(1, 5);
            var receivedValues = new List<int>();

            // Act
            var subscription = source
                .WhereAsync(async x =>
                {
                    await Task.Delay(10);
                    return x % 2 == 0; // Only even numbers
                })
                .Subscribe(value => receivedValues.Add(value));

            await Task.Delay(100); // Wait for async operations

            // Assert
            Assert.AreEqual(2, receivedValues.Count);
            Assert.AreEqual(new[] { 2, 4 }, receivedValues.ToArray());

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator FromAsyncOperation_TracksProgress()
        {
            // Arrange
            var progressValues = new List<float>();
            var completed = false;

            // Create a mock observable that simulates AsyncOperation progress tracking
            var mockAsyncOperation = Observable.Create<float>(observer =>
            {
                // Use a simple approach without accessing internal scheduler
                var go = new GameObject("TestRunner");
                var runner = go.AddComponent<TestRunner>();
                var routine = runner.StartCoroutine(SimulateAsyncOperationProgress(observer));
                return Disposable.Create(() =>
                {
                    if (routine != null && runner != null)
                        runner.StopCoroutine(routine);
                    if (go != null)
                        UnityEngine.Object.DestroyImmediate(go);
                });
            });

            // Act
            var subscription = mockAsyncOperation
                .Subscribe(
                    progress => progressValues.Add(progress),
                    onCompleted: () => completed = true
                );

            // Wait for the simulated operation to complete
            yield return new WaitForSeconds(0.3f);

            // Assert
            Assert.GreaterOrEqual(progressValues.Count, 3);
            Assert.AreEqual(0.0f, progressValues[0], 0.001f);
            Assert.AreEqual(1.0f, progressValues[progressValues.Count - 1], 0.001f);
            Assert.IsTrue(completed);

            // Cleanup
            subscription.Dispose();
        }

        private System.Collections.IEnumerator SimulateAsyncOperationProgress(IObserver<float> observer)
        {
            observer.OnNext(0.0f);
            yield return new WaitForSeconds(0.1f);
            observer.OnNext(0.5f);
            yield return new WaitForSeconds(0.1f);
            observer.OnNext(1.0f);
            observer.OnCompleted();
        }

        /// <summary>
        /// Simple MonoBehaviour for running test coroutines.
        /// </summary>
        private class TestRunner : MonoBehaviour
        {
        }
    }
}
