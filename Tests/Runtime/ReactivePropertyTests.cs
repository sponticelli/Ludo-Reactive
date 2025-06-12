using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Unit tests for ReactiveProperty functionality.
    /// </summary>
    public class ReactivePropertyTests
    {
        [Test]
        public void ReactiveProperty_InitialValue_IsCorrect()
        {
            // Arrange
            var initialValue = 42;
            var property = new ReactiveProperty<int>(initialValue);

            // Act & Assert
            Assert.AreEqual(initialValue, property.Value);
        }

        [Test]
        public void ReactiveProperty_SetValue_NotifiesObservers()
        {
            // Arrange
            var property = new ReactiveProperty<int>(0);
            var receivedValues = new List<int>();
            
            property.Subscribe(value => receivedValues.Add(value));

            // Act
            property.Value = 10;
            property.Value = 20;

            // Assert
            Assert.AreEqual(3, receivedValues.Count); // Initial + 2 changes
            Assert.AreEqual(0, receivedValues[0]);   // Initial value
            Assert.AreEqual(10, receivedValues[1]);  // First change
            Assert.AreEqual(20, receivedValues[2]);  // Second change
        }

        [Test]
        public void ReactiveProperty_SetSameValue_DoesNotNotify()
        {
            // Arrange
            var property = new ReactiveProperty<int>(10);
            var notificationCount = 0;
            
            property.Subscribe(_ => notificationCount++);

            // Act
            property.Value = 10; // Same value
            property.Value = 10; // Same value again

            // Assert
            Assert.AreEqual(1, notificationCount); // Only initial notification
        }

        [Test]
        public void ReactiveProperty_Dispose_StopsNotifications()
        {
            // Arrange
            var property = new ReactiveProperty<int>(0);
            var receivedValues = new List<int>();
            
            var subscription = property.Subscribe(value => receivedValues.Add(value));

            // Act
            subscription.Dispose();
            property.Value = 10;

            // Assert
            Assert.AreEqual(1, receivedValues.Count); // Only initial value
            Assert.AreEqual(0, receivedValues[0]);
        }

        [Test]
        public void ReactiveProperty_MultipleSubscribers_AllNotified()
        {
            // Arrange
            var property = new ReactiveProperty<string>("initial");
            var subscriber1Values = new List<string>();
            var subscriber2Values = new List<string>();
            
            property.Subscribe(value => subscriber1Values.Add(value));
            property.Subscribe(value => subscriber2Values.Add(value));

            // Act
            property.Value = "changed";

            // Assert
            Assert.AreEqual(2, subscriber1Values.Count);
            Assert.AreEqual(2, subscriber2Values.Count);
            Assert.AreEqual("initial", subscriber1Values[0]);
            Assert.AreEqual("changed", subscriber1Values[1]);
            Assert.AreEqual("initial", subscriber2Values[0]);
            Assert.AreEqual("changed", subscriber2Values[1]);
        }

        [Test]
        public void ReactiveProperty_ForceNotify_NotifiesEvenWithSameValue()
        {
            // Arrange
            var property = new ReactiveProperty<int>(10);
            var notificationCount = 0;
            
            property.Subscribe(_ => notificationCount++);

            // Act
            property.ForceNotify();

            // Assert
            Assert.AreEqual(2, notificationCount); // Initial + forced notification
        }

        [Test]
        public void ReactiveProperty_SetValueWithoutNotify_DoesNotNotify()
        {
            // Arrange
            var property = new ReactiveProperty<int>(0);
            var notificationCount = 0;
            
            property.Subscribe(_ => notificationCount++);

            // Act
            property.SetValueWithoutNotify(10);

            // Assert
            Assert.AreEqual(1, notificationCount); // Only initial notification
            Assert.AreEqual(10, property.Value);   // But value is changed
        }

        [Test]
        public void ReactiveProperty_AsReadOnly_PreventsDirectModification()
        {
            // Arrange
            var property = new ReactiveProperty<int>(42);
            var readOnly = property.AsReadOnly();

            // Act & Assert
            Assert.AreEqual(42, readOnly.Value);
            Assert.AreEqual(property.HasObservers, readOnly.HasObservers);
            Assert.AreEqual(property.ObserverCount, readOnly.ObserverCount);
            
            // Verify it's truly read-only (no setter available)
            var readOnlyType = readOnly.GetType();
            var valueProperty = readOnlyType.GetProperty("Value");
            Assert.IsNull(valueProperty.GetSetMethod());
        }

        [Test]
        public void ReactiveProperty_ImplicitConversion_ReturnsValue()
        {
            // Arrange
            var property = new ReactiveProperty<int>(42);

            // Act
            int value = property; // Implicit conversion

            // Assert
            Assert.AreEqual(42, value);
        }

        [Test]
        public void ReactiveProperty_ToString_ReturnsValueString()
        {
            // Arrange
            var property = new ReactiveProperty<string>("test");

            // Act
            var result = property.ToString();

            // Assert
            Assert.AreEqual("test", result);
        }

        [Test]
        public void ReactiveProperty_NullValue_HandledCorrectly()
        {
            // Arrange
            var property = new ReactiveProperty<string>(null);
            var receivedValues = new List<string>();
            
            property.Subscribe(value => receivedValues.Add(value));

            // Act
            property.Value = "not null";
            property.Value = null;

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.IsNull(receivedValues[0]);
            Assert.AreEqual("not null", receivedValues[1]);
            Assert.IsNull(receivedValues[2]);
        }

        [Test]
        public void ReactiveProperty_CustomEqualityComparer_UsedForComparison()
        {
            // Arrange
            var comparer = StringComparer.OrdinalIgnoreCase;
            var property = new ReactiveProperty<string>("Hello", comparer);
            var notificationCount = 0;
            
            property.Subscribe(_ => notificationCount++);

            // Act
            property.Value = "HELLO"; // Same value with different case

            // Assert
            Assert.AreEqual(1, notificationCount); // Should not notify due to custom comparer
        }

        [Test]
        public void ReactiveProperty_ObserverException_DoesNotBreakOtherObservers()
        {
            // Arrange
            var property = new ReactiveProperty<int>(0);
            var goodObserverNotified = false;

            // Expect error during subscription (when bad observer gets initial value)
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Observer OnNext threw exception during subscription.*"));
            property.Subscribe(_ => throw new Exception("Bad observer"));

            property.Subscribe(_ => goodObserverNotified = true);

            // Act - Expect error during value change
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Observer OnNext threw exception.*"));
            property.Value = 10;

            // Assert
            Assert.IsTrue(goodObserverNotified, "Good observer should still be notified despite exception in bad observer");
        }
    }
}
