using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Unit tests for Observable creation and basic operators.
    /// </summary>
    public class ObservableTests
    {
        [Test]
        public void Observable_Return_EmitsSingleValue()
        {
            // Arrange
            var expectedValue = 42;
            var receivedValues = new List<int>();
            var completed = false;

            // Act
            Observable.Return(expectedValue)
                .Subscribe(
                    value => receivedValues.Add(value),
                    onCompleted: () => completed = true
                );

            // Assert
            Assert.AreEqual(1, receivedValues.Count);
            Assert.AreEqual(expectedValue, receivedValues[0]);
            Assert.IsTrue(completed);
        }

        [Test]
        public void Observable_Empty_CompletesImmediately()
        {
            // Arrange
            var receivedValues = new List<int>();
            var completed = false;

            // Act
            Observable.Empty<int>()
                .Subscribe(
                    value => receivedValues.Add(value),
                    onCompleted: () => completed = true
                );

            // Assert
            Assert.AreEqual(0, receivedValues.Count);
            Assert.IsTrue(completed);
        }

        [Test]
        public void Observable_Never_NeverEmitsOrCompletes()
        {
            // Arrange
            var receivedValues = new List<int>();
            var completed = false;
            var errored = false;

            // Act
            var subscription = Observable.Never<int>()
                .Subscribe(
                    value => receivedValues.Add(value),
                    error => errored = true,
                    onCompleted: () => completed = true
                );

            // Assert
            Assert.AreEqual(0, receivedValues.Count);
            Assert.IsFalse(completed);
            Assert.IsFalse(errored);
            
            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void Observable_Throw_EmitsError()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            var receivedValues = new List<int>();
            var receivedException = default(Exception);
            var completed = false;

            // Act
            Observable.Throw<int>(expectedException)
                .Subscribe(
                    value => receivedValues.Add(value),
                    error => receivedException = error,
                    onCompleted: () => completed = true
                );

            // Assert
            Assert.AreEqual(0, receivedValues.Count);
            Assert.IsFalse(completed);
            Assert.AreSame(expectedException, receivedException);
        }

        [Test]
        public void Observable_Create_WorksWithCustomLogic()
        {
            // Arrange
            var receivedValues = new List<int>();
            var completed = false;

            // Act
            Observable.Create<int>(observer =>
            {
                observer.OnNext(1);
                observer.OnNext(2);
                observer.OnNext(3);
                observer.OnCompleted();
                return Disposable.Empty;
            })
            .Subscribe(
                value => receivedValues.Add(value),
                onCompleted: () => completed = true
            );

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 2, 3 }, receivedValues.ToArray());
            Assert.IsTrue(completed);
        }

        [Test]
        public void Observable_Select_TransformsValues()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var receivedValues = new List<string>();

            // Act
            source.ToObservable()
                .Select(x => $"Value: {x}")
                .Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual("Value: 1", receivedValues[0]);
            Assert.AreEqual("Value: 2", receivedValues[1]);
            Assert.AreEqual("Value: 3", receivedValues[2]);
        }

        [Test]
        public void Observable_Where_FiltersValues()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5 };
            var receivedValues = new List<int>();

            // Act
            source.ToObservable()
                .Where(x => x % 2 == 0) // Even numbers only
                .Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(2, receivedValues.Count);
            Assert.AreEqual(new[] { 2, 4 }, receivedValues.ToArray());
        }

        [Test]
        public void Observable_Take_LimitsValues()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5 };
            var receivedValues = new List<int>();
            var completed = false;

            // Act
            source.ToObservable()
                .Take(3)
                .Subscribe(
                    value => receivedValues.Add(value),
                    onCompleted: () => completed = true
                );

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 2, 3 }, receivedValues.ToArray());
            Assert.IsTrue(completed);
        }

        [Test]
        public void Observable_DistinctUntilChanged_SkipsDuplicates()
        {
            // Arrange
            var source = new[] { 1, 1, 2, 2, 2, 3, 1 };
            var receivedValues = new List<int>();

            // Act
            source.ToObservable()
                .DistinctUntilChanged()
                .Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(4, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 2, 3, 1 }, receivedValues.ToArray());
        }

        [Test]
        public void Observable_Do_PerformsSideEffects()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var sideEffectValues = new List<int>();
            var receivedValues = new List<int>();

            // Act
            source.ToObservable()
                .Do(value => sideEffectValues.Add(value * 10))
                .Subscribe(value => receivedValues.Add(value));

            // Assert
            Assert.AreEqual(new[] { 1, 2, 3 }, receivedValues.ToArray());
            Assert.AreEqual(new[] { 10, 20, 30 }, sideEffectValues.ToArray());
        }

        [Test]
        public void Observable_CombineLatest_CombinesLatestValues()
        {
            // Arrange
            var subject1 = new Subject<int>();
            var subject2 = new Subject<string>();
            var receivedValues = new List<string>();

            // Act
            subject1.CombineLatest(subject2, (i, s) => $"{i}-{s}")
                .Subscribe(value => receivedValues.Add(value));

            subject1.OnNext(1);
            subject2.OnNext("A");
            subject1.OnNext(2);
            subject2.OnNext("B");

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual("1-A", receivedValues[0]);
            Assert.AreEqual("2-A", receivedValues[1]);
            Assert.AreEqual("2-B", receivedValues[2]);
        }

        [Test]
        public void Observable_Merge_MergesMultipleStreams()
        {
            // Arrange
            var subject1 = new Subject<int>();
            var subject2 = new Subject<int>();
            var receivedValues = new List<int>();

            // Act
            Observable.Merge(subject1, subject2)
                .Subscribe(value => receivedValues.Add(value));

            subject1.OnNext(1);
            subject2.OnNext(10);
            subject1.OnNext(2);
            subject2.OnNext(20);

            // Assert
            Assert.AreEqual(4, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 10, 2, 20 }, receivedValues.ToArray());
        }

        [Test]
        public void Observable_ToObservable_ConvertsEnumerable()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var receivedValues = new List<int>();
            var completed = false;

            // Act
            source.ToObservable()
                .Subscribe(
                    value => receivedValues.Add(value),
                    onCompleted: () => completed = true
                );

            // Assert
            Assert.AreEqual(3, receivedValues.Count);
            Assert.AreEqual(new[] { 1, 2, 3 }, receivedValues.ToArray());
            Assert.IsTrue(completed);
        }

        [Test]
        public void Observable_Generate_GeneratesSequence()
        {
            // Arrange
            var receivedValues = new List<int>();
            var completed = false;

            // Act
            Observable.Generate(
                initialState: 0,
                condition: x => x < 5,
                iterate: x => x + 1,
                resultSelector: x => x * 2
            )
            .Subscribe(
                value => receivedValues.Add(value),
                onCompleted: () => completed = true
            );

            // Assert
            Assert.AreEqual(5, receivedValues.Count);
            Assert.AreEqual(new[] { 0, 2, 4, 6, 8 }, receivedValues.ToArray());
            Assert.IsTrue(completed);
        }
    }
}
