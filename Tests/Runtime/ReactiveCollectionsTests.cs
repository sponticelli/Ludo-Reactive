using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ludo.Reactive.Collections;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Tests for the reactive collections system in Ludo.Reactive v1.2.0.
    /// </summary>
    [TestFixture]
    public class ReactiveCollectionsTests
    {
        [Test]
        public void ObservableList_Add_EmitsChangeSet()
        {
            // Arrange
            var list = new ObservableList<string>();
            CollectionChangeSet<string> receivedChangeSet = null;
            list.Subscribe(changeSet => receivedChangeSet = changeSet);

            // Act
            list.Add("item1");

            // Assert
            Assert.IsNotNull(receivedChangeSet);
            Assert.AreEqual(1, receivedChangeSet.Count);
            Assert.IsTrue(receivedChangeSet.HasChanges(CollectionChangeType.Add));
            
            var addedItems = receivedChangeSet.GetAddedItems().ToList();
            Assert.AreEqual(1, addedItems.Count);
            Assert.AreEqual(0, addedItems[0].Index);
            Assert.AreEqual("item1", addedItems[0].Item);
        }

        [Test]
        public void ObservableList_AddRange_EmitsBatchChangeSet()
        {
            // Arrange
            var list = new ObservableList<string>();
            CollectionChangeSet<string> receivedChangeSet = null;
            list.Subscribe(changeSet => receivedChangeSet = changeSet);

            // Act
            list.AddRange(new[] { "item1", "item2", "item3" });

            // Assert
            Assert.IsNotNull(receivedChangeSet);
            Assert.AreEqual(3, receivedChangeSet.Count);
            Assert.IsTrue(receivedChangeSet.HasChanges(CollectionChangeType.Add));
            
            var addedItems = receivedChangeSet.GetAddedItems().ToList();
            Assert.AreEqual(3, addedItems.Count);
            Assert.AreEqual("item1", addedItems[0].Item);
            Assert.AreEqual("item2", addedItems[1].Item);
            Assert.AreEqual("item3", addedItems[2].Item);
        }

        [Test]
        public void ObservableList_Remove_EmitsChangeSet()
        {
            // Arrange
            var list = new ObservableList<string> { "item1", "item2" };
            CollectionChangeSet<string> receivedChangeSet = null;
            list.Subscribe(changeSet => receivedChangeSet = changeSet);

            // Act
            list.Remove("item1");

            // Assert
            Assert.IsNotNull(receivedChangeSet);
            Assert.AreEqual(1, receivedChangeSet.Count);
            Assert.IsTrue(receivedChangeSet.HasChanges(CollectionChangeType.Remove));
            
            var removedItems = receivedChangeSet.GetRemovedItems().ToList();
            Assert.AreEqual(1, removedItems.Count);
            Assert.AreEqual(0, removedItems[0].Index);
            Assert.AreEqual("item1", removedItems[0].Item);
        }

        [Test]
        public void ObservableList_Replace_EmitsChangeSet()
        {
            // Arrange
            var list = new ObservableList<string> { "item1", "item2" };
            CollectionChangeSet<string> receivedChangeSet = null;
            list.Subscribe(changeSet => receivedChangeSet = changeSet);

            // Act
            list[0] = "newItem";

            // Assert
            Assert.IsNotNull(receivedChangeSet);
            Assert.AreEqual(1, receivedChangeSet.Count);
            Assert.IsTrue(receivedChangeSet.HasChanges(CollectionChangeType.Replace));
            
            var replacedItems = receivedChangeSet.GetReplacedItems().ToList();
            Assert.AreEqual(1, replacedItems.Count);
            Assert.AreEqual(0, replacedItems[0].Index);
            Assert.AreEqual("item1", replacedItems[0].OldValue);
            Assert.AreEqual("newItem", replacedItems[0].NewValue);
        }

        [Test]
        public void ObservableDictionary_Add_EmitsChangeEvent()
        {
            // Arrange
            var dict = new ObservableDictionary<string, int>();
            DictionaryChangeEvent<string, int>? receivedEvent = null;
            dict.Subscribe(evt => receivedEvent = evt);

            // Act
            dict.Add("key1", 42);

            // Assert
            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CollectionChangeType.Add, receivedEvent.Value.Type);
            Assert.AreEqual("key1", receivedEvent.Value.Key);
            Assert.AreEqual(42, receivedEvent.Value.NewValue);
        }

        [Test]
        public void ObservableDictionary_Update_EmitsReplaceEvent()
        {
            // Arrange
            var dict = new ObservableDictionary<string, int> { { "key1", 10 } };
            DictionaryChangeEvent<string, int>? receivedEvent = null;
            dict.Subscribe(evt => receivedEvent = evt);

            // Act
            dict["key1"] = 20;

            // Assert
            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CollectionChangeType.Replace, receivedEvent.Value.Type);
            Assert.AreEqual("key1", receivedEvent.Value.Key);
            Assert.AreEqual(10, receivedEvent.Value.OldValue);
            Assert.AreEqual(20, receivedEvent.Value.NewValue);
        }

        [Test]
        public void ObservableDictionary_Remove_EmitsRemoveEvent()
        {
            // Arrange
            var dict = new ObservableDictionary<string, int> { { "key1", 42 } };
            DictionaryChangeEvent<string, int>? receivedEvent = null;
            dict.Subscribe(evt => receivedEvent = evt);

            // Act
            dict.Remove("key1");

            // Assert
            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CollectionChangeType.Remove, receivedEvent.Value.Type);
            Assert.AreEqual("key1", receivedEvent.Value.Key);
            Assert.AreEqual(42, receivedEvent.Value.OldValue);
        }

        [Test]
        public void ObservableSet_Add_EmitsChangeEvent()
        {
            // Arrange
            var set = new ObservableSet<string>();
            SetChangeEvent<string>? receivedEvent = null;
            set.Subscribe(evt => receivedEvent = evt);

            // Act
            var added = set.Add("item1");

            // Assert
            Assert.IsTrue(added);
            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CollectionChangeType.Add, receivedEvent.Value.Type);
            Assert.AreEqual("item1", receivedEvent.Value.Item);
        }

        [Test]
        public void ObservableSet_AddDuplicate_DoesNotEmitEvent()
        {
            // Arrange
            var set = new ObservableSet<string> { "item1" };
            SetChangeEvent<string>? receivedEvent = null;
            set.Subscribe(evt => receivedEvent = evt);

            // Act
            var added = set.Add("item1");

            // Assert
            Assert.IsFalse(added);
            Assert.IsNull(receivedEvent);
        }

        [Test]
        public void ObservableSet_UnionWith_EmitsMultipleAddEvents()
        {
            // Arrange
            var set = new ObservableSet<string> { "item1" };
            var addEvents = new List<SetChangeEvent<string>>();
            set.ObserveAdd().Subscribe(evt => addEvents.Add(evt));

            // Act
            set.UnionWith(new[] { "item1", "item2", "item3" });

            // Assert
            Assert.AreEqual(2, addEvents.Count); // Only item2 and item3 should be added
            Assert.IsTrue(addEvents.Any(e => e.Item == "item2"));
            Assert.IsTrue(addEvents.Any(e => e.Item == "item3"));
        }

        [Test]
        public void CollectionSynchronizer_AddCollection_SynchronizesChanges()
        {
            // Arrange
            var synchronizer = new CollectionSynchronizer<string>();
            var list1 = new ObservableList<string> { "item1" };
            var list2 = new ObservableList<string>();

            // Act
            synchronizer.AddCollection(list1, "List1");
            synchronizer.AddCollection(list2, "List2");
            list1.Add("item2");

            // Assert
            Assert.AreEqual(2, list2.Count);
            Assert.AreEqual("item1", list2[0]);
            Assert.AreEqual("item2", list2[1]);
        }

        [Test]
        public void CollectionSynchronizer_SynchronizeToSource_UpdatesAllCollections()
        {
            // Arrange
            var synchronizer = new CollectionSynchronizer<string>();
            var source = new ObservableList<string> { "a", "b", "c" };
            var target1 = new ObservableList<string> { "x", "y" };
            var target2 = new ObservableList<string>();

            synchronizer.AddCollection(source, "Source");
            synchronizer.AddCollection(target1, "Target1");
            synchronizer.AddCollection(target2, "Target2");

            // Act
            synchronizer.SynchronizeToSource(source);

            // Assert
            Assert.AreEqual(3, target1.Count);
            Assert.AreEqual(3, target2.Count);
            Assert.AreEqual("a", target1[0]);
            Assert.AreEqual("b", target1[1]);
            Assert.AreEqual("c", target1[2]);
        }

        [Test]
        public void CollectionDiffer_ComputeDiff_FindsCorrectOperations()
        {
            // Arrange
            var differ = new CollectionDiffer<string>();
            var source = new List<string> { "a", "b", "c" };
            var target = new List<string> { "a", "x", "c", "d" };

            // Act
            var operations = differ.ComputeDiff(source, target);

            // Assert
            Assert.IsTrue(operations.Count > 0);
            
            // Apply operations to verify correctness
            var result = new List<string>(source);
            differ.ApplyDiff(result, operations);
            
            Assert.AreEqual(target.Count, result.Count);
            for (int i = 0; i < target.Count; i++)
            {
                Assert.AreEqual(target[i], result[i]);
            }
        }

        [Test]
        public void CollectionDiffer_ComputeSimpleDiff_HandlesAppendScenario()
        {
            // Arrange
            var differ = new CollectionDiffer<string>();
            var source = new List<string> { "a", "b" };
            var target = new List<string> { "a", "b", "c", "d" };

            // Act
            var operations = differ.ComputeSimpleDiff(source, target);

            // Assert
            Assert.AreEqual(2, operations.Count);
            Assert.IsTrue(operations.All(op => op.Type == DiffOperationType.Insert));
            Assert.AreEqual("c", operations[0].Item);
            Assert.AreEqual("d", operations[1].Item);
        }

        [Test]
        public void CollectionDiffer_ComputeLCS_FindsLongestCommonSubsequence()
        {
            // Arrange
            var differ = new CollectionDiffer<string>();
            var source = new List<string> { "a", "b", "c", "d" };
            var target = new List<string> { "a", "x", "c", "y", "d" };

            // Act
            var lcs = differ.ComputeLCS(source, target);

            // Assert
            Assert.AreEqual(3, lcs.Count);
            Assert.AreEqual("a", lcs[0]);
            Assert.AreEqual("c", lcs[1]);
            Assert.AreEqual("d", lcs[2]);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any disposable resources created during tests
        }
    }
}
