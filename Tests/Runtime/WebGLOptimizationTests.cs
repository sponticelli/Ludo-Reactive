using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ludo.Reactive.WebGL;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Unit tests for WebGL optimizations.
    /// </summary>
    public class WebGLOptimizationTests
    {
        [TearDown]
        public void TearDown()
        {
            WebGLOptimizations.ClearCaches();
        }

        [Test]
        public void WebGLOptimizations_CreateOptimized_CreatesObservable()
        {
            // Arrange
            var receivedValues = new List<int>();

            // Act
            var observable = WebGLOptimizations.CreateOptimized<int>(observer =>
            {
                observer.OnNext(42);
                observer.OnCompleted();
                return Disposable.Empty;
            });

            var subscription = observable.Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual(42, receivedValues[0]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void WebGLOptimizations_CreateCached_ReusesInstance()
        {
            // Arrange
            var factoryCallCount = 0;
            
            // Act
            var observable1 = WebGLOptimizations.CreateCached<int>(() =>
            {
                factoryCallCount++;
                return Observable.Return(42);
            });

            var observable2 = WebGLOptimizations.CreateCached<int>(() =>
            {
                factoryCallCount++;
                return Observable.Return(84);
            });

            // Assert
            Assert.AreSame(observable1, observable2);
            Assert.AreEqual(1, factoryCallCount); // Factory should only be called once
        }

        [Test]
        public void WebGLOptimizations_CreateCached_DifferentTypesGetDifferentInstances()
        {
            // Act
            var intObservable = WebGLOptimizations.CreateCached<int>(() => Observable.Return(42));
            var stringObservable = WebGLOptimizations.CreateCached<string>(() => Observable.Return("test"));

            // Assert
            Assert.AreNotSame(intObservable, stringObservable);
        }

        [UnityTest]
        public IEnumerator WebGLOptimizations_IntervalOptimized_EmitsValues()
        {
            // Arrange
            var receivedValues = new List<long>();
            var period = TimeSpan.FromSeconds(0.1f);

            // Act
            var observable = WebGLOptimizations.IntervalOptimized(period);
            var subscription = observable.Subscribe(value => receivedValues.Add(value));

            // Wait for a few emissions
            yield return new WaitForSeconds(0.35f);

            // Assert
            Assert.GreaterOrEqual(receivedValues.Count, 2);
            Assert.AreEqual(0, receivedValues[0]);
            Assert.AreEqual(1, receivedValues[1]);

            // Cleanup
            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator WebGLOptimizations_TimerOptimized_EmitsAfterDelay()
        {
            // Arrange
            var receivedValues = new List<long>();
            var completed = false;
            var dueTime = TimeSpan.FromSeconds(0.2f);

            // Act
            var observable = WebGLOptimizations.TimerOptimized(dueTime);
            var subscription = observable.Subscribe(
                value => receivedValues.Add(value),
                onCompleted: () => completed = true
            );

            // Wait less than due time
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(0, receivedValues.Count);

            // Wait for due time
            yield return new WaitForSeconds(0.15f);

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual(0, receivedValues[0]);
            Assert.IsTrue(completed);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void WebGLOptimizations_OptimizeForWebGL_WrapsObservable()
        {
            // Arrange
            var source = Observable.Return(42);
            var receivedValues = new List<int>();

            // Act
            var optimized = source.OptimizeForWebGL();
            var subscription = optimized.Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual(42, receivedValues[0]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void WebGLOptimizations_OptimizeForWebGL_HandlesExceptions()
        {
            // Arrange
            var source = Observable.Create<int>(observer =>
            {
                observer.OnNext(42);
                return Disposable.Empty;
            });

            var receivedValues = new List<int>();
            var receivedErrors = new List<Exception>();

            // Act
            var optimized = source.OptimizeForWebGL();
            var subscription = optimized.Subscribe(
                value =>
                {
                    receivedValues.Add(value);
                    if (value == 42)
                        throw new Exception("Test exception");
                },
                error => receivedErrors.Add(error)
            );

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual(1, receivedErrors.Count);
            Assert.AreEqual("Test exception", receivedErrors[0].Message);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void WebGLOptimizations_BatchForWebGL_BatchesBySize()
        {
            // Arrange
            var source = new Subject<int>();
            var receivedBatches = new List<IList<int>>();
            var batchSize = 3;

            // Act
            var batched = source.BatchForWebGL(batchSize, TimeSpan.FromMinutes(1));
            var subscription = batched.Subscribe(batch => receivedBatches.Add(batch));

            // Send values
            source.OnNext(1);
            source.OnNext(2);
            source.OnNext(3); // Should trigger batch

            source.OnNext(4);
            source.OnNext(5);

            // Assert
            Assert.AreEqual(1, receivedBatches.Count);
            Assert.AreEqual(3, receivedBatches[0].Count);
            Assert.AreEqual(1, receivedBatches[0][0]);
            Assert.AreEqual(2, receivedBatches[0][1]);
            Assert.AreEqual(3, receivedBatches[0][2]);

            // Cleanup
            subscription.Dispose();
            source.Dispose();
        }

        [Test]
        public void WebGLOptimizations_BatchForWebGL_EmitsOnCompletion()
        {
            // Arrange
            var source = new Subject<int>();
            var receivedBatches = new List<IList<int>>();
            var completed = false;

            // Act
            var batched = source.BatchForWebGL(5, TimeSpan.FromMinutes(1));
            var subscription = batched.Subscribe(
                batch => receivedBatches.Add(batch),
                onCompleted: () => completed = true
            );

            // Send values and complete
            source.OnNext(1);
            source.OnNext(2);
            source.OnCompleted(); // Should emit partial batch

            // Assert
            Assert.AreEqual(1, receivedBatches.Count);
            Assert.AreEqual(2, receivedBatches[0].Count);
            Assert.IsTrue(completed);

            // Cleanup
            subscription.Dispose();
            source.Dispose();
        }

        [Test]
        public void WebGLOptimizations_BatchForWebGL_HandlesErrors()
        {
            // Arrange
            var source = new Subject<int>();
            var receivedBatches = new List<IList<int>>();
            var receivedErrors = new List<Exception>();

            // Act
            var batched = source.BatchForWebGL(5, TimeSpan.FromMinutes(1));
            var subscription = batched.Subscribe(
                batch => receivedBatches.Add(batch),
                error => receivedErrors.Add(error)
            );

            // Send values and error
            source.OnNext(1);
            source.OnNext(2);
            source.OnError(new Exception("Test error"));

            // Assert
            Assert.AreEqual(0, receivedBatches.Count); // No batch should be emitted on error
            Assert.AreEqual(1, receivedErrors.Count);
            Assert.AreEqual("Test error", receivedErrors[0].Message);

            // Cleanup
            subscription.Dispose();
            source.Dispose();
        }

        [Test]
        public void WebGLOptimizations_ClearCaches_ClearsAllCaches()
        {
            // Arrange
            var factoryCallCount = 0;
            var observable1 = WebGLOptimizations.CreateCached<int>(() =>
            {
                factoryCallCount++;
                return Observable.Return(42);
            });

            // Act
            WebGLOptimizations.ClearCaches();

            var observable2 = WebGLOptimizations.CreateCached<int>(() =>
            {
                factoryCallCount++;
                return Observable.Return(42);
            });

            // Assert
            Assert.AreNotSame(observable1, observable2);
            Assert.AreEqual(2, factoryCallCount); // Factory should be called again after clear
        }

        [Test]
        public void WebGLOptimizedObservable_Subscribe_HandlesNullObserver()
        {
            // Arrange
            var observable = WebGLOptimizations.CreateOptimized<int>(observer =>
            {
                observer.OnNext(42);
                return Disposable.Empty;
            });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => observable.Subscribe(null));
        }

        [Test]
        public void WebGLOptimizedObservable_Subscribe_HandlesExceptionInSubscribe()
        {
            // Arrange
            var receivedErrors = new List<Exception>();
            var observable = WebGLOptimizations.CreateOptimized<int>(observer =>
            {
                throw new Exception("Subscribe error");
            });

            // Act
            var subscription = observable.Subscribe(
                value => { },
                error => receivedErrors.Add(error)
            );

            // Assert
            Assert.AreEqual(1, receivedErrors.Count);
            Assert.AreEqual("Subscribe error", receivedErrors[0].Message);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void WebGLOptimizations_BatchForWebGL_InvalidParameters_ThrowsException()
        {
            // Arrange
            var source = Observable.Return(42);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                WebGLOptimizations.BatchForWebGL<int>(null, 1, TimeSpan.FromSeconds(1)));
            
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                source.BatchForWebGL(0, TimeSpan.FromSeconds(1)));
            
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                source.BatchForWebGL(-1, TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void WebGLOptimizations_CreateOptimized_NullSubscribe_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                WebGLOptimizations.CreateOptimized<int>(null));
        }

        [Test]
        public void WebGLOptimizations_CreateCached_NullFactory_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                WebGLOptimizations.CreateCached<int>(null));
        }

        [Test]
        public void WebGLOptimizations_OptimizeForWebGL_NullSource_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                WebGLOptimizations.OptimizeForWebGL<int>(null));
        }
    }
}
