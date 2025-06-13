using System;
using System.Collections.Generic;
using NUnit.Framework;
using Ludo.Reactive;

namespace Ludo.Reactive.Tests
{
    [TestFixture]
    public class ZipOperatorTests
    {
        private List<IDisposable> _disposables;
        
        [SetUp]
        public void SetUp()
        {
            _disposables = new List<IDisposable>();
        }
        
        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }
        
        private void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }
        
        [Test]
        public void Zip_PairsElementsByIndex()
        {
            var source1 = new Subject<int>();
            var source2 = new Subject<string>();
            var results = new List<string>();
            
            AddDisposable(source1
                .Zip(source2, (i, s) => $"{i}-{s}")
                .Subscribe(results.Add));
            
            source1.OnNext(1);
            // No emission yet - need both sources at same index
            Assert.AreEqual(0, results.Count);
            
            source2.OnNext("A");
            // Now we have both values at index 0
            Assert.AreEqual(new[] { "1-A" }, results);
            
            source1.OnNext(2);
            source1.OnNext(3);
            // Still only one emission - waiting for second source
            Assert.AreEqual(new[] { "1-A" }, results);
            
            source2.OnNext("B");
            // Now we have pair at index 1
            Assert.AreEqual(new[] { "1-A", "2-B" }, results);
            
            source2.OnNext("C");
            // Now we have pair at index 2
            Assert.AreEqual(new[] { "1-A", "2-B", "3-C" }, results);
        }
        
        [Test]
        public void Zip_CompletesWhenEitherSourceCompletes()
        {
            var source1 = new Subject<int>();
            var source2 = new Subject<string>();
            var results = new List<string>();
            var completed = false;
            
            AddDisposable(source1
                .Zip(source2, (i, s) => $"{i}-{s}")
                .Subscribe(
                    results.Add,
                    () => completed = true
                ));
            
            source1.OnNext(1);
            source2.OnNext("A");
            Assert.AreEqual(new[] { "1-A" }, results);
            Assert.IsFalse(completed);
            
            source1.OnNext(2);
            source2.OnNext("B");
            Assert.AreEqual(new[] { "1-A", "2-B" }, results);
            Assert.IsFalse(completed);
            
            // Complete first source
            source1.OnCompleted();
            Assert.IsTrue(completed);
            
            // Further emissions from second source should be ignored
            source2.OnNext("C");
            Assert.AreEqual(new[] { "1-A", "2-B" }, results);
        }
        
        [Test]
        public void Zip_PropagatesErrors()
        {
            var source1 = new Subject<int>();
            var source2 = new Subject<string>();
            var results = new List<string>();
            var errorReceived = false;
            Exception receivedException = null;
            
            AddDisposable(source1
                .Zip(source2, (i, s) => $"{i}-{s}")
                .Subscribe(
                    results.Add,
                    error => 
                    {
                        errorReceived = true;
                        receivedException = error;
                    }
                ));
            
            source1.OnNext(1);
            source2.OnNext("A");
            Assert.AreEqual(new[] { "1-A" }, results);
            
            var testException = new InvalidOperationException("Test error");
            source1.OnError(testException);
            
            Assert.IsTrue(errorReceived);
            Assert.AreEqual(testException, receivedException);
        }
        
        [Test]
        public void Zip_HandlesResultSelectorExceptions()
        {
            var source1 = new Subject<int>();
            var source2 = new Subject<string>();
            var errorReceived = false;
            Exception receivedException = null;
            
            AddDisposable(source1
                .Zip(source2, (i, s) => 
                {
                    if (i == 2) throw new InvalidOperationException("Selector error");
                    return $"{i}-{s}";
                })
                .Subscribe(
                    _ => { },
                    error => 
                    {
                        errorReceived = true;
                        receivedException = error;
                    }
                ));
            
            source1.OnNext(1);
            source2.OnNext("A");
            // Should work fine
            
            source1.OnNext(2);
            source2.OnNext("B");
            // Should trigger exception in selector
            
            Assert.IsTrue(errorReceived);
            Assert.IsInstanceOf<InvalidOperationException>(receivedException);
            Assert.AreEqual("Selector error", receivedException.Message);
        }
        
        [Test]
        public void Zip_WithNullArguments_ThrowsArgumentNullException()
        {
            var source = new Subject<int>();
            
            Assert.Throws<ArgumentNullException>(() => 
                ((IObservable<int>)null).Zip(source, (a, b) => a + b));
            
            Assert.Throws<ArgumentNullException>(() => 
                source.Zip((IObservable<int>)null, (a, b) => a + b));
            
            Assert.Throws<ArgumentNullException>(() => 
                source.Zip(source, (Func<int, int, int>)null));
        }
        
        [Test]
        public void Zip_EmptySequences_CompletesImmediately()
        {
            var source1 = Observable.Empty<int>();
            var source2 = Observable.Empty<string>();
            var results = new List<string>();
            var completed = false;
            
            AddDisposable(source1
                .Zip(source2, (i, s) => $"{i}-{s}")
                .Subscribe(
                    results.Add,
                    () => completed = true
                ));
            
            Assert.AreEqual(0, results.Count);
            Assert.IsTrue(completed);
        }
        
        [Test]
        public void Zip_DifferentSequenceLengths_CompletesWithShorter()
        {
            var source1 = Observable.Create<int>(observer =>
            {
                observer.OnNext(1);
                observer.OnNext(2);
                observer.OnNext(3);
                observer.OnCompleted();
                return Disposable.Empty;
            });
            
            var source2 = Observable.Create<string>(observer =>
            {
                observer.OnNext("A");
                observer.OnNext("B");
                observer.OnCompleted(); // Shorter sequence
                return Disposable.Empty;
            });
            
            var results = new List<string>();
            var completed = false;
            
            AddDisposable(source1
                .Zip(source2, (i, s) => $"{i}-{s}")
                .Subscribe(
                    results.Add,
                    () => completed = true
                ));
            
            Assert.AreEqual(new[] { "1-A", "2-B" }, results);
            Assert.IsTrue(completed);
        }
    }
}
