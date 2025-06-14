using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Unit tests for DisposableTracker component functionality.
    /// </summary>
    public class DisposableTrackerTests
    {
        private GameObject _testGameObject;
        private DisposableTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestDisposableTracker");
            _tracker = _testGameObject.AddComponent<DisposableTracker>();
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
        public void Add_IncreasesCount()
        {
            // Arrange
            var disposable1 = Disposable.Create(() => { });
            var disposable2 = Disposable.Create(() => { });

            // Act
            _tracker.Add(disposable1);
            _tracker.Add(disposable2);

            // Assert
            Assert.AreEqual(2, _tracker.Count);
        }

        [Test]
        public void Add_NullDisposable_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tracker.Add(null));
        }

        [Test]
        public void Remove_DecreasesCount()
        {
            // Arrange
            var disposable1 = Disposable.Create(() => { });
            var disposable2 = Disposable.Create(() => { });
            _tracker.Add(disposable1);
            _tracker.Add(disposable2);

            // Act
            var removed = _tracker.Remove(disposable1);

            // Assert
            Assert.IsTrue(removed);
            Assert.AreEqual(1, _tracker.Count);
        }

        [Test]
        public void Remove_NonExistentDisposable_ReturnsFalse()
        {
            // Arrange
            var disposable1 = Disposable.Create(() => { });
            var disposable2 = Disposable.Create(() => { });
            _tracker.Add(disposable1);

            // Act
            var removed = _tracker.Remove(disposable2);

            // Assert
            Assert.IsFalse(removed);
            Assert.AreEqual(1, _tracker.Count);
        }

        [Test]
        public void Remove_NullDisposable_ReturnsFalse()
        {
            // Act
            var removed = _tracker.Remove(null);

            // Assert
            Assert.IsFalse(removed);
        }

        [Test]
        public void DisposeAll_DisposesAllTrackedDisposables()
        {
            // Arrange
            var disposed1 = false;
            var disposed2 = false;
            var disposed3 = false;

            var disposable1 = Disposable.Create(() => disposed1 = true);
            var disposable2 = Disposable.Create(() => disposed2 = true);
            var disposable3 = Disposable.Create(() => disposed3 = true);

            _tracker.Add(disposable1);
            _tracker.Add(disposable2);
            _tracker.Add(disposable3);

            // Act
            _tracker.DisposeAll();

            // Assert
            Assert.IsTrue(disposed1);
            Assert.IsTrue(disposed2);
            Assert.IsTrue(disposed3);
            Assert.AreEqual(0, _tracker.Count);
        }

        [Test]
        public void DisposeAll_CanBeCalledMultipleTimes()
        {
            // Arrange
            var disposeCount = 0;
            var disposable = Disposable.Create(() => disposeCount++);
            _tracker.Add(disposable);

            // Act
            _tracker.DisposeAll();
            _tracker.DisposeAll();
            _tracker.DisposeAll();

            // Assert
            Assert.AreEqual(1, disposeCount, "Should only dispose once");
            Assert.AreEqual(0, _tracker.Count);
        }

        [Test]
        public void Clear_RemovesAllWithoutDisposing()
        {
            // Arrange
            var disposed1 = false;
            var disposed2 = false;

            var disposable1 = Disposable.Create(() => disposed1 = true);
            var disposable2 = Disposable.Create(() => disposed2 = true);

            _tracker.Add(disposable1);
            _tracker.Add(disposable2);

            // Act
            _tracker.Clear();

            // Assert
            Assert.IsFalse(disposed1);
            Assert.IsFalse(disposed2);
            Assert.AreEqual(0, _tracker.Count);
        }

        [Test]
        public void OnDestroy_DisposesAllTrackedDisposables()
        {
            // Arrange
            var disposed1 = false;
            var disposed2 = false;

            var disposable1 = Disposable.Create(() => disposed1 = true);
            var disposable2 = Disposable.Create(() => disposed2 = true);

            _tracker.Add(disposable1);
            _tracker.Add(disposable2);

            // Act
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            _testGameObject = null;

            // Assert
            Assert.IsTrue(disposed1);
            Assert.IsTrue(disposed2);
        }

        [UnityTest]
        public IEnumerator OnApplicationPause_DisposesAllTrackedDisposables()
        {
            // Arrange
            var disposed1 = false;
            var disposed2 = false;

            var disposable1 = Disposable.Create(() => disposed1 = true);
            var disposable2 = Disposable.Create(() => disposed2 = true);

            _tracker.Add(disposable1);
            _tracker.Add(disposable2);

            // Act - Simulate application pause
            _tracker.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);
            yield return null;

            // Assert
            Assert.IsTrue(disposed1);
            Assert.IsTrue(disposed2);
        }

        [UnityTest]
        public IEnumerator OnApplicationFocus_DisposesAllTrackedDisposables()
        {
            // Arrange
            var disposed1 = false;
            var disposed2 = false;

            var disposable1 = Disposable.Create(() => disposed1 = true);
            var disposable2 = Disposable.Create(() => disposed2 = true);

            _tracker.Add(disposable1);
            _tracker.Add(disposable2);

            // Act - Simulate application focus lost
            _tracker.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            yield return null;

            // Assert
            Assert.IsTrue(disposed1);
            Assert.IsTrue(disposed2);
        }

        [Test]
        public void ThreadSafety_ConcurrentAddRemove_HandledCorrectly()
        {
            // Arrange
            var disposables = new List<IDisposable>();
            for (int i = 0; i < 100; i++)
            {
                disposables.Add(Disposable.Create(() => { }));
            }

            // Act - Simulate concurrent operations
            System.Threading.Tasks.Parallel.For(0, 50, i =>
            {
                _tracker.Add(disposables[i]);
            });

            System.Threading.Tasks.Parallel.For(50, 100, i =>
            {
                _tracker.Add(disposables[i]);
            });

            // Remove some concurrently
            System.Threading.Tasks.Parallel.For(0, 25, i =>
            {
                _tracker.Remove(disposables[i]);
            });

            // Assert
            Assert.AreEqual(75, _tracker.Count);
        }

        [Test]
        public void AddTo_Extension_AddsToTracker()
        {
            // Arrange
            var disposed = false;
            var disposable = Disposable.Create(() => disposed = true);

            // Act
            disposable.AddTo(_tracker.gameObject);

            // Assert
            Assert.AreEqual(1, _tracker.Count);

            // Cleanup and verify disposal
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            _testGameObject = null;
            Assert.IsTrue(disposed);
        }

        [Test]
        public void AddTo_Extension_CreatesTrackerIfNotExists()
        {
            // Arrange
            var newGameObject = new GameObject("NewTestObject");
            var disposed = false;
            var disposable = Disposable.Create(() => disposed = true);

            // Act
            disposable.AddTo(newGameObject);

            // Assert
            var tracker = newGameObject.GetComponent<DisposableTracker>();
            Assert.IsNotNull(tracker);
            Assert.AreEqual(1, tracker.Count);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(newGameObject);
            Assert.IsTrue(disposed);
        }

        [Test]
        public void AddTo_Extension_WithComponent_AddsToGameObject()
        {
            // Arrange
            var component = _testGameObject.AddComponent<TestComponent>();
            var disposed = false;
            var disposable = Disposable.Create(() => disposed = true);

            // Act
            disposable.AddTo(component);

            // Assert
            Assert.AreEqual(1, _tracker.Count);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            _testGameObject = null;
            Assert.IsTrue(disposed);
        }

        [Test]
        public void AddTo_Extension_NullArguments_ThrowsException()
        {
            // Arrange
            var disposable = Disposable.Create(() => { });
            GameObject nullGameObject = null;
            Component nullComponent = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => disposable.AddTo(nullGameObject));
            Assert.Throws<ArgumentNullException>(() => disposable.AddTo(nullComponent));
            Assert.Throws<ArgumentNullException>(() => ((IDisposable)null).AddTo(_testGameObject));
        }

        [Test]
        public void DisposableTracker_HandleExceptionInDispose_ContinuesDisposingOthers()
        {
            // Arrange
            var disposed1 = false;
            var disposed3 = false;

            var disposable1 = Disposable.Create(() => disposed1 = true);
            var disposable2 = Disposable.Create(() => throw new Exception("Test exception"));
            var disposable3 = Disposable.Create(() => disposed3 = true);

            _tracker.Add(disposable1);
            _tracker.Add(disposable2);
            _tracker.Add(disposable3);

            // Expect the error log message (using regex to match the beginning)
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"^\[Ludo\.Reactive\] Exception while disposing tracked disposable: System\.Exception: Test exception"));

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _tracker.DisposeAll());

            // Assert
            Assert.IsTrue(disposed1);
            Assert.IsTrue(disposed3);
            Assert.AreEqual(0, _tracker.Count);
        }

        /// <summary>
        /// Test component for testing AddTo with components.
        /// </summary>
        private class TestComponent : MonoBehaviour
        {
        }
    }
}
