using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Unit tests for ObjectPool system.
    /// </summary>
    public class ObjectPoolTests
    {
        private ObjectPool<TestPooledObject> _pool;

        [SetUp]
        public void SetUp()
        {
            _pool = new ObjectPool<TestPooledObject>(
                () => new TestPooledObject(),
                obj => obj.Reset(),
                maxSize: 5
            );
        }

        [TearDown]
        public void TearDown()
        {
            _pool?.Dispose();
        }

        [Test]
        public void ObjectPool_Get_CreatesNewObjectWhenEmpty()
        {
            // Act
            var obj = _pool.Get();

            // Assert
            Assert.IsNotNull(obj);
            Assert.AreEqual(0, _pool.Count);
        }

        [Test]
        public void ObjectPool_ReturnAndGet_ReusesObject()
        {
            // Arrange
            var obj1 = _pool.Get();
            obj1.Value = 42;

            // Act
            _pool.Return(obj1);
            var obj2 = _pool.Get();

            // Assert
            Assert.AreSame(obj1, obj2);
            Assert.AreEqual(0, obj2.Value); // Should be reset
            Assert.AreEqual(0, _pool.Count);
        }

        [Test]
        public void ObjectPool_Count_TracksCorrectly()
        {
            // Arrange
            var obj1 = _pool.Get();
            var obj2 = _pool.Get();

            // Act & Assert
            Assert.AreEqual(0, _pool.Count);

            _pool.Return(obj1);
            Assert.AreEqual(1, _pool.Count);

            _pool.Return(obj2);
            Assert.AreEqual(2, _pool.Count);

            _pool.Get();
            Assert.AreEqual(1, _pool.Count);
        }

        [Test]
        public void ObjectPool_MaxSize_LimitsPoolSize()
        {
            // Arrange
            var objects = new List<TestPooledObject>();
            for (int i = 0; i < 10; i++)
            {
                objects.Add(_pool.Get());
            }

            // Act - Return more objects than max size
            foreach (var obj in objects)
            {
                _pool.Return(obj);
            }

            // Assert - Pool should not exceed max size
            Assert.AreEqual(5, _pool.Count);
            Assert.AreEqual(5, _pool.MaxSize);
        }

        [Test]
        public void ObjectPool_ReturnNull_DoesNothing()
        {
            // Act
            _pool.Return(null);

            // Assert
            Assert.AreEqual(0, _pool.Count);
        }

        [Test]
        public void ObjectPool_Clear_RemovesAllObjects()
        {
            // Arrange
            _pool.Return(new TestPooledObject());
            _pool.Return(new TestPooledObject());
            Assert.AreEqual(2, _pool.Count);

            // Act
            _pool.Clear();

            // Assert
            Assert.AreEqual(0, _pool.Count);
        }

        [Test]
        public void ObjectPool_Dispose_CleansUpResources()
        {
            // Arrange
            _pool.Return(new TestPooledObject());

            // Act
            _pool.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => _pool.Get());
            Assert.Throws<ObjectDisposedException>(() => _pool.Return(new TestPooledObject()));
        }

        [Test]
        public void ObjectPool_ResetAction_CalledOnReturn()
        {
            // Arrange
            var resetCalled = false;
            var customPool = new ObjectPool<TestPooledObject>(
                () => new TestPooledObject(),
                obj => resetCalled = true,
                maxSize: 5
            );

            var obj = customPool.Get();

            // Act
            customPool.Return(obj);

            // Assert
            Assert.IsTrue(resetCalled);

            // Cleanup
            customPool.Dispose();
        }

        [Test]
        public void ObjectPool_ExceptionInResetAction_DoesNotBreakPool()
        {
            // Arrange
            var customPool = new ObjectPool<TestPooledObject>(
                () => new TestPooledObject(),
                obj => throw new Exception("Reset error"),
                maxSize: 5
            );

            var obj = customPool.Get();

            // Temporarily ignore failing messages for this test
            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                // Act & Assert - Should not throw
                Assert.DoesNotThrow(() => customPool.Return(obj));

                // Pool should still work
                var newObj = customPool.Get();
                Assert.IsNotNull(newObj);
            }
            finally
            {
                // Restore previous setting
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;

                // Cleanup
                customPool.Dispose();
            }
        }

        [Test]
        public void ReactiveObjectPools_DisposableList_Works()
        {
            // Act
            var list = ReactiveObjectPools.GetDisposableList();
            list.Add(Disposable.Empty);
            ReactiveObjectPools.ReturnDisposableList(list);

            var list2 = ReactiveObjectPools.GetDisposableList();

            // Assert
            Assert.AreSame(list, list2);
            Assert.AreEqual(0, list2.Count); // Should be cleared
        }

        [Test]
        public void ReactiveObjectPools_ObjectList_Works()
        {
            // Act
            var list = ReactiveObjectPools.GetObjectList();
            list.Add("test");
            ReactiveObjectPools.ReturnObjectList(list);

            var list2 = ReactiveObjectPools.GetObjectList();

            // Assert
            Assert.AreSame(list, list2);
            Assert.AreEqual(0, list2.Count); // Should be cleared
        }

        [Test]
        public void ReactiveObjectPools_TypeDictionary_Works()
        {
            // Act
            var dict = ReactiveObjectPools.GetTypeDictionary();
            dict[typeof(string)] = "test";
            ReactiveObjectPools.ReturnTypeDictionary(dict);

            var dict2 = ReactiveObjectPools.GetTypeDictionary();

            // Assert
            Assert.AreSame(dict, dict2);
            Assert.AreEqual(0, dict2.Count); // Should be cleared
        }

        [Test]
        public void ReactiveObjectPools_ClearAll_ClearsAllPools()
        {
            // Arrange
            var list = ReactiveObjectPools.GetDisposableList();
            ReactiveObjectPools.ReturnDisposableList(list);

            // Act
            ReactiveObjectPools.ClearAll();

            // Assert - Getting a new list should create a new instance
            var newList = ReactiveObjectPools.GetDisposableList();
            Assert.IsNotNull(newList);
        }

        [Test]
        public void ObjectPoolExtensions_CreateReturnDisposable_Works()
        {
            // Arrange
            var obj = _pool.Get();
            obj.Value = 42;

            // Act
            var disposable = _pool.CreateReturnDisposable(obj);
            disposable.Dispose();

            // Assert
            Assert.AreEqual(1, _pool.Count);
            
            var returnedObj = _pool.Get();
            Assert.AreSame(obj, returnedObj);
            Assert.AreEqual(0, returnedObj.Value); // Should be reset
        }

        [Test]
        public void ObjectPoolExtensions_GetWithAutoReturn_Works()
        {
            // Act
            var disposable = _pool.GetWithAutoReturn(out var obj);
            obj.Value = 42;
            disposable.Dispose();

            // Assert
            Assert.AreEqual(1, _pool.Count);
            
            var returnedObj = _pool.Get();
            Assert.AreSame(obj, returnedObj);
            Assert.AreEqual(0, returnedObj.Value); // Should be reset
        }

        [Test]
        public void PooledDisposable_Create_Works()
        {
            // Arrange
            var actionCalled = false;

            // Act
            var disposable = PooledDisposable.Create(() => actionCalled = true);
            disposable.Dispose();

            // Assert
            Assert.IsTrue(actionCalled);
        }

        [Test]
        public void PooledDisposable_Reuse_Works()
        {
            // Arrange
            var actionCalled1 = false;
            var actionCalled2 = false;

            // Act
            var disposable1 = PooledDisposable.Create(() => actionCalled1 = true);
            disposable1.Dispose();

            var disposable2 = PooledDisposable.Create(() => actionCalled2 = true);
            disposable2.Dispose();

            // Assert
            Assert.IsTrue(actionCalled1);
            Assert.IsTrue(actionCalled2);
            // Note: We can't easily test if the same instance was reused without reflection
        }

        [Test]
        public void PooledDisposable_DoubleDispose_DoesNotThrow()
        {
            // Arrange
            var actionCallCount = 0;
            var disposable = PooledDisposable.Create(() => actionCallCount++);

            // Act & Assert
            disposable.Dispose();
            Assert.DoesNotThrow(() => disposable.Dispose());
            Assert.AreEqual(1, actionCallCount); // Should only be called once
        }

        [Test]
        public void PooledDisposable_ExceptionInAction_DoesNotBreakPool()
        {
            // Arrange
            var disposable1 = PooledDisposable.Create(() => throw new Exception("Test error"));
            var actionCalled = false;

            // Temporarily ignore failing messages for this test
            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                // Act & Assert
                Assert.DoesNotThrow(() => disposable1.Dispose());

                // Pool should still work
                var disposable2 = PooledDisposable.Create(() => actionCalled = true);
                disposable2.Dispose();
                Assert.IsTrue(actionCalled);
            }
            finally
            {
                // Restore previous setting
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
        }

        // Test helper class
        private class TestPooledObject
        {
            public int Value { get; set; }

            public void Reset()
            {
                Value = 0;
            }
        }
    }
}
