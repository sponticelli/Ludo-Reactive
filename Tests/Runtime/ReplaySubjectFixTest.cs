using NUnit.Framework;
using System.Collections.Generic;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Test to verify the ReplaySubject DateTime overflow fix.
    /// </summary>
    public class ReplaySubjectFixTest
    {
        [Test]
        public void ReplaySubject_WithLargeBufferSize_DoesNotThrowDateTimeOverflow()
        {
            // Arrange
            var subject = new ReplaySubject<int>(1000000); // Large buffer size
            var receivedValues = new List<int>();

            // Act - This should not throw a DateTime overflow exception
            subject.OnNext(1);
            subject.OnNext(2);
            subject.OnNext(3);

            var subscription = subject.Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 2, 3 }, receivedValues.ToArray());

            // Cleanup
            subscription.Dispose();
            subject.Dispose();
        }

        [Test]
        public void ReplayOperator_DoesNotThrowDateTimeOverflow()
        {
            // Arrange
            var source = Observable.Range(1, 5);
            var receivedValues = new List<int>();

            // Act - This should not throw a DateTime overflow exception
            var connectable = source.Replay();
            var subscription = connectable.Subscribe(value => receivedValues.Add(value));
            var connection = connectable.Connect();

            // Assert
            Assert.AreEqual(5, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, receivedValues.ToArray());

            // Cleanup
            subscription.Dispose();
            connection.Dispose();
        }
    }
}
