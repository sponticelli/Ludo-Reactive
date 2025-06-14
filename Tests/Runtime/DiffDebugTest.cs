using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Ludo.Reactive.Collections;

namespace Ludo.Reactive.Tests
{
    public class DiffDebugTest
    {
        [Test]
        public void DebugDiffOperations()
        {
            // Arrange
            var differ = new CollectionDiffer<string>();
            var source = new List<string> { "a", "b", "c" };
            var target = new List<string> { "a", "x", "c", "d" };

            Debug.Log("Source: [" + string.Join(", ", source) + "]");
            Debug.Log("Target: [" + string.Join(", ", target) + "]");

            // Act
            var operations = differ.ComputeDiff(source, target);
            
            Debug.Log("\nOperations:");
            foreach (var op in operations)
            {
                Debug.Log($"  {op}");
            }

            // Apply operations to verify correctness
            var result = new List<string>(source);
            differ.ApplyDiff(result, operations);
            
            Debug.Log("\nResult: [" + string.Join(", ", result) + "]");
            Debug.Log("Expected: [" + string.Join(", ", target) + "]");
            
            // Assert
            Assert.AreEqual(target.Count, result.Count);
            for (int i = 0; i < target.Count; i++)
            {
                Assert.AreEqual(target[i], result[i], $"Mismatch at index {i}");
            }
        }
    }
}
