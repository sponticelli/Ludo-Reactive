using System.Collections.Generic;
using NUnit.Framework;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Basic functionality test to ensure the system works after fixing compilation errors.
    /// </summary>
    public class BasicFunctionalityTest
    {
        [Test]
        public void ReactiveProperty_BasicUsage_Works()
        {
            // Arrange
            var property = new ReactiveProperty<int>(10);
            var receivedValues = new List<int>();

            // Act
            var subscription = property.Subscribe(value => receivedValues.Add(value));
            property.Value = 20;
            property.Value = 30;

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(10, receivedValues[0]); // Initial value
            Assert.AreEqual(20, receivedValues[1]); // First change
            Assert.AreEqual(30, receivedValues[2]); // Second change

            // Cleanup
            subscription.Dispose();
            property.Dispose();
        }

        [Test]
        public void Observable_Select_Works()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var receivedValues = new List<string>();

            // Act
            var subscription = source.ToObservable()
                .Select(x => $"Value: {x}")
                .Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual("Value: 1", receivedValues[0]);
            Assert.AreEqual("Value: 2", receivedValues[1]);
            Assert.AreEqual("Value: 3", receivedValues[2]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void Subject_BasicUsage_Works()
        {
            // Arrange
            var subject = new Subject<string>();
            var receivedValues = new List<string>();

            // Act
            var subscription = subject.Subscribe(value => receivedValues.Add(value));
            subject.OnNext("Hello");
            subject.OnNext("World");

            // Assert
            Assert.AreEqual(2, receivedValues.Count);
            Assert.AreEqual("Hello", receivedValues[0]);
            Assert.AreEqual("World", receivedValues[1]);

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }

        [Test]
        public void BehaviorSubject_LateSubscriber_GetsCurrentValue()
        {
            // Arrange
            var subject = new BehaviorSubject<int>(42);
            var receivedValues = new List<int>();

            // Act
            subject.OnNext(100);
            var subscription = subject.Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual(100, receivedValues[0]); // Should get current value

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }

        [Test]
        public void Observable_Where_FiltersCorrectly()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5 };
            var receivedValues = new List<int>();

            // Act
            var subscription = source.ToObservable()
                .Where(x => x % 2 == 0)
                .Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(2, receivedValues.Count);
            Assert.AreEqual(2, receivedValues[0]);
            Assert.AreEqual(4, receivedValues[1]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void Observable_CombineLatest_Works()
        {
            // Arrange
            var subject1 = new Subject<int>();
            var subject2 = new Subject<string>();
            var receivedValues = new List<string>();

            // Act
            var subscription = subject1.CombineLatest(subject2, (i, s) => $"{i}-{s}")
                .Subscribe(value => receivedValues.Add(value));

            subject1.OnNext(1);
            subject2.OnNext("A");
            subject1.OnNext(2);

            // Assert
            Assert.AreEqual(2, receivedValues.Count);
            Assert.AreEqual("1-A", receivedValues[0]);
            Assert.AreEqual("2-A", receivedValues[1]);

            // Cleanup
            subscription.Dispose();
            subject1.Dispose();
            subject2.Dispose();
        }

        [Test]
        public void CompositeDisposable_DisposesAll()
        {
            // Arrange
            var composite = new CompositeDisposable();
            var disposed1 = false;
            var disposed2 = false;

            composite.Add(Disposable.Create(() => disposed1 = true));
            composite.Add(Disposable.Create(() => disposed2 = true));

            // Act
            composite.Dispose();

            // Assert
            Assert.IsTrue(disposed1);
            Assert.IsTrue(disposed2);
        }
    }
}
