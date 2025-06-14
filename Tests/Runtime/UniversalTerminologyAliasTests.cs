using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ludo.Reactive;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Tests for universal reactive programming terminology aliases (Map, FlatMap, Fold)
    /// to ensure they work identically to their original counterparts (Select, SelectMany, Scan).
    /// </summary>
    [TestFixture]
    public class UniversalTerminologyAliasTests
    {
        #region Map (Select) Tests

        [Test]
        public void Map_TransformsValues_IdenticalToSelect()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var mapResults = new List<string>();
            var selectResults = new List<string>();

            // Act
            source.ToObservable()
                .Map(x => $"Value: {x}")
                .Subscribe(value => mapResults.Add(value));

            source.ToObservable()
                .Select(x => $"Value: {x}")
                .Subscribe(value => selectResults.Add(value));

            // Assert
            Assert.AreEqual(selectResults.Count, mapResults.Count);
            Assert.AreEqual(selectResults.ToArray(), mapResults.ToArray());
            Assert.AreEqual(3, mapResults.Count);
            Assert.AreEqual("Value: 1", mapResults[0]);
            Assert.AreEqual("Value: 2", mapResults[1]);
            Assert.AreEqual("Value: 3", mapResults[2]);
        }

        [Test]
        public void Map_WithComplexTransformation_IdenticalToSelect()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5 };
            var mapResults = new List<int>();
            var selectResults = new List<int>();

            // Act
            source.ToObservable()
                .Map(x => x * x + 1)
                .Subscribe(value => mapResults.Add(value));

            source.ToObservable()
                .Select(x => x * x + 1)
                .Subscribe(value => selectResults.Add(value));

            // Assert
            Assert.AreEqual(selectResults.ToArray(), mapResults.ToArray());
            Assert.AreEqual(new[] { 2, 5, 10, 17, 26 }, mapResults.ToArray());
        }

        [Test]
        public void Map_WithException_BehavesIdenticalToSelect()
        {
            // Arrange
            var source = new[] { 1, 2, 0, 4 }; // Division by zero on third element
            var mapErrors = new List<Exception>();
            var selectErrors = new List<Exception>();

            // Act
            source.ToObservable()
                .Map(x => 10 / x)
                .Subscribe(
                    onNext: _ => { },
                    onError: ex => mapErrors.Add(ex)
                );

            source.ToObservable()
                .Select(x => 10 / x)
                .Subscribe(
                    onNext: _ => { },
                    onError: ex => selectErrors.Add(ex)
                );

            // Assert
            Assert.AreEqual(1, mapErrors.Count);
            Assert.AreEqual(1, selectErrors.Count);
            Assert.AreEqual(selectErrors[0].GetType(), mapErrors[0].GetType());
        }

        #endregion

        #region FlatMap (SelectMany) Tests

        [Test]
        public void FlatMap_FlattensObservables_IdenticalToSelectMany()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var flatMapResults = new List<int>();
            var selectManyResults = new List<int>();

            // Act
            source.ToObservable()
                .FlatMap(x => Observable.Range(x, 2)) // For each x, emit x and x+1
                .Subscribe(value => flatMapResults.Add(value));

            source.ToObservable()
                .SelectMany(x => Observable.Range(x, 2))
                .Subscribe(value => selectManyResults.Add(value));

            // Assert
            Assert.AreEqual(selectManyResults.ToArray(), flatMapResults.ToArray());
            Assert.AreEqual(6, flatMapResults.Count); // 2 values for each of 3 inputs
        }

        [Test]
        public void FlatMap_WithAsyncOperations_IdenticalToSelectMany()
        {
            // Arrange
            var source = new[] { "A", "B", "C" };
            var flatMapResults = new List<string>();
            var selectManyResults = new List<string>();
            var flatMapCompleted = false;
            var selectManyCompleted = false;

            // Act
            source.ToObservable()
                .FlatMap(x => Observable.Return($"Processed-{x}"))
                .Subscribe(
                    value => flatMapResults.Add(value),
                    onCompleted: () => flatMapCompleted = true
                );

            source.ToObservable()
                .SelectMany(x => Observable.Return($"Processed-{x}"))
                .Subscribe(
                    value => selectManyResults.Add(value),
                    onCompleted: () => selectManyCompleted = true
                );

            // Assert
            Assert.AreEqual(selectManyResults.ToArray(), flatMapResults.ToArray());
            Assert.AreEqual(flatMapCompleted, selectManyCompleted);
            Assert.IsTrue(flatMapCompleted);
            Assert.AreEqual(new[] { "Processed-A", "Processed-B", "Processed-C" }, flatMapResults.ToArray());
        }

        #endregion

        #region Fold (Scan) Tests

        [Test]
        public void Fold_AccumulatesValues_IdenticalToScan()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5 };
            var foldResults = new List<int>();
            var scanResults = new List<int>();

            // Act
            source.ToObservable()
                .Fold(0, (acc, x) => acc + x)
                .Subscribe(value => foldResults.Add(value));

            source.ToObservable()
                .Scan(0, (acc, x) => acc + x)
                .Subscribe(value => scanResults.Add(value));

            // Assert
            Assert.AreEqual(scanResults.ToArray(), foldResults.ToArray());
            Assert.AreEqual(new[] { 1, 3, 6, 10, 15 }, foldResults.ToArray()); // Running sum
        }

        [Test]
        public void Fold_WithComplexAccumulator_IdenticalToScan()
        {
            // Arrange
            var source = new[] { "A", "B", "C" };
            var foldResults = new List<string>();
            var scanResults = new List<string>();

            // Act
            source.ToObservable()
                .Fold("", (acc, x) => acc + x + "-")
                .Subscribe(value => foldResults.Add(value));

            source.ToObservable()
                .Scan("", (acc, x) => acc + x + "-")
                .Subscribe(value => scanResults.Add(value));

            // Assert
            Assert.AreEqual(scanResults.ToArray(), foldResults.ToArray());
            Assert.AreEqual(new[] { "A-", "A-B-", "A-B-C-" }, foldResults.ToArray());
        }

        [Test]
        public void Fold_WithDifferentTypes_IdenticalToScan()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4 };
            var foldResults = new List<string>();
            var scanResults = new List<string>();

            // Act
            source.ToObservable()
                .Fold("Start", (acc, x) => $"{acc}[{x}]")
                .Subscribe(value => foldResults.Add(value));

            source.ToObservable()
                .Scan("Start", (acc, x) => $"{acc}[{x}]")
                .Subscribe(value => scanResults.Add(value));

            // Assert
            Assert.AreEqual(scanResults.ToArray(), foldResults.ToArray());
            Assert.AreEqual(new[] { "Start[1]", "Start[1][2]", "Start[1][2][3]", "Start[1][2][3][4]" }, foldResults.ToArray());
        }

        #endregion

        #region Filter (Where) Tests

        [Test]
        public void Filter_FiltersValues_IdenticalToWhere()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5 };
            var filterResults = new List<int>();
            var whereResults = new List<int>();

            // Act
            source.ToObservable()
                .Filter(x => x % 2 == 0) // Even numbers only
                .Subscribe(value => filterResults.Add(value));

            source.ToObservable()
                .Where(x => x % 2 == 0) // Even numbers only
                .Subscribe(value => whereResults.Add(value));

            // Assert
            Assert.AreEqual(whereResults.ToArray(), filterResults.ToArray());
            Assert.AreEqual(new[] { 2, 4 }, filterResults.ToArray());
        }

        [Test]
        public void Filter_WithComplexPredicate_IdenticalToWhere()
        {
            // Arrange
            var source = new[] { "apple", "banana", "cherry", "date", "elderberry" };
            var filterResults = new List<string>();
            var whereResults = new List<string>();

            // Act
            source.ToObservable()
                .Filter(x => x.Length > 5)
                .Subscribe(value => filterResults.Add(value));

            source.ToObservable()
                .Where(x => x.Length > 5)
                .Subscribe(value => whereResults.Add(value));

            // Assert
            Assert.AreEqual(whereResults.ToArray(), filterResults.ToArray());
            Assert.AreEqual(new[] { "banana", "cherry", "elderberry" }, filterResults.ToArray());
        }

        #endregion

        #region Tap (Do) Tests

        [Test]
        public void Tap_PerformsSideEffects_IdenticalToDo()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var tapSideEffects = new List<int>();
            var doSideEffects = new List<int>();
            var tapResults = new List<int>();
            var doResults = new List<int>();

            // Act
            source.ToObservable()
                .Tap(value => tapSideEffects.Add(value * 10))
                .Subscribe(value => tapResults.Add(value));

            source.ToObservable()
                .Do(value => doSideEffects.Add(value * 10))
                .Subscribe(value => doResults.Add(value));

            // Assert
            Assert.AreEqual(doResults.ToArray(), tapResults.ToArray());
            Assert.AreEqual(doSideEffects.ToArray(), tapSideEffects.ToArray());
            Assert.AreEqual(new[] { 1, 2, 3 }, tapResults.ToArray());
            Assert.AreEqual(new[] { 10, 20, 30 }, tapSideEffects.ToArray());
        }

        [Test]
        public void Tap_WithLifecycleEvents_IdenticalToDo()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var tapEvents = new List<string>();
            var doEvents = new List<string>();

            // Act
            source.ToObservable()
                .Tap(
                    onNext: x => tapEvents.Add($"Next: {x}"),
                    onError: ex => tapEvents.Add($"Error: {ex.Message}"),
                    onCompleted: () => tapEvents.Add("Completed")
                )
                .Subscribe(_ => { });

            source.ToObservable()
                .Do(
                    onNext: x => doEvents.Add($"Next: {x}"),
                    onError: ex => doEvents.Add($"Error: {ex.Message}"),
                    onCompleted: () => doEvents.Add("Completed")
                )
                .Subscribe(_ => { });

            // Assert
            Assert.AreEqual(doEvents.ToArray(), tapEvents.ToArray());
            Assert.AreEqual(new[] { "Next: 1", "Next: 2", "Next: 3", "Completed" }, tapEvents.ToArray());
        }

        #endregion

        #region Reduce (Aggregate) Tests

        [Test]
        public void Reduce_WithSeedAndResultSelector_IdenticalToAggregate()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5 };
            var reduceResults = new List<string>();
            var aggregateResults = new List<string>();

            // Act
            source.ToObservable()
                .Reduce(0, (acc, x) => acc + x, acc => $"Sum: {acc}")
                .Subscribe(value => reduceResults.Add(value));

            source.ToObservable()
                .Aggregate(0, (acc, x) => acc + x, acc => $"Sum: {acc}")
                .Subscribe(value => aggregateResults.Add(value));

            // Assert
            Assert.AreEqual(aggregateResults.ToArray(), reduceResults.ToArray());
            Assert.AreEqual(new[] { "Sum: 15" }, reduceResults.ToArray());
        }

        [Test]
        public void Reduce_WithoutSeed_IdenticalToAggregate()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5 };
            var reduceResults = new List<int>();
            var aggregateResults = new List<int>();

            // Act
            source.ToObservable()
                .Reduce((acc, x) => acc + x)
                .Subscribe(value => reduceResults.Add(value));

            source.ToObservable()
                .Aggregate((acc, x) => acc + x)
                .Subscribe(value => aggregateResults.Add(value));

            // Assert
            Assert.AreEqual(aggregateResults.ToArray(), reduceResults.ToArray());
            Assert.AreEqual(new[] { 15 }, reduceResults.ToArray());
        }

        #endregion

        #region Of (Return) Tests

        [Test]
        public void Of_CreatesSingleValueObservable_IdenticalToReturn()
        {
            // Arrange
            var ofResults = new List<int>();
            var returnResults = new List<int>();
            var ofCompleted = false;
            var returnCompleted = false;

            // Act
            Observable.Of(42)
                .Subscribe(
                    value => ofResults.Add(value),
                    onCompleted: () => ofCompleted = true
                );

            Observable.Return(42)
                .Subscribe(
                    value => returnResults.Add(value),
                    onCompleted: () => returnCompleted = true
                );

            // Assert
            Assert.AreEqual(returnResults.ToArray(), ofResults.ToArray());
            Assert.AreEqual(ofCompleted, returnCompleted);
            Assert.IsTrue(ofCompleted);
            Assert.AreEqual(new[] { 42 }, ofResults.ToArray());
        }

        [Test]
        public void Of_WithComplexType_IdenticalToReturn()
        {
            // Arrange
            var testObject = new { Name = "Test", Value = 123 };
            var ofResults = new List<object>();
            var returnResults = new List<object>();

            // Act
            Observable.Of(testObject)
                .Subscribe(value => ofResults.Add(value));

            Observable.Return(testObject)
                .Subscribe(value => returnResults.Add(value));

            // Assert
            Assert.AreEqual(returnResults.ToArray(), ofResults.ToArray());
            Assert.AreEqual(1, ofResults.Count);
            Assert.AreEqual(testObject, ofResults[0]);
        }

        #endregion

        #region Debounce (Throttle) Tests

        [Test]
        public void Debounce_LimitsEmissionRate_IdenticalToThrottle()
        {
            // Arrange
            var source = Observable.Create<int>(observer =>
            {
                observer.OnNext(1);
                observer.OnNext(2);
                observer.OnNext(3);
                observer.OnCompleted();
                return Disposable.Empty;
            });

            var debounceResults = new List<int>();
            var throttleResults = new List<int>();

            // Act
            source.Debounce(TimeSpan.FromMilliseconds(100))
                .Subscribe(value => debounceResults.Add(value));

            source.Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(value => throttleResults.Add(value));

            // Assert
            Assert.AreEqual(throttleResults.ToArray(), debounceResults.ToArray());
        }

        [Test]
        public void Debounce_WithScheduler_IdenticalToThrottle()
        {
            // Arrange
            var scheduler = UnitySchedulers.MainThread;
            var source = Observable.Range(1, 3);
            var debounceResults = new List<int>();
            var throttleResults = new List<int>();

            // Act
            source.Debounce(TimeSpan.FromMilliseconds(50), scheduler)
                .Subscribe(value => debounceResults.Add(value));

            source.Throttle(TimeSpan.FromMilliseconds(50), scheduler)
                .Subscribe(value => throttleResults.Add(value));

            // Assert
            Assert.AreEqual(throttleResults.ToArray(), debounceResults.ToArray());
        }

        #endregion

        #region Distinct (DistinctUntilChanged) Tests

        [Test]
        public void Distinct_SkipsConsecutiveDuplicates_IdenticalToDistinctUntilChanged()
        {
            // Arrange
            var source = new[] { 1, 1, 2, 2, 2, 3, 1, 1 };
            var distinctResults = new List<int>();
            var distinctUntilChangedResults = new List<int>();

            // Act
            source.ToObservable()
                .Distinct()
                .Subscribe(value => distinctResults.Add(value));

            source.ToObservable()
                .DistinctUntilChanged()
                .Subscribe(value => distinctUntilChangedResults.Add(value));

            // Assert
            Assert.AreEqual(distinctUntilChangedResults.ToArray(), distinctResults.ToArray());
            Assert.AreEqual(new[] { 1, 2, 3, 1 }, distinctResults.ToArray());
        }

        [Test]
        public void Distinct_WithCustomComparer_IdenticalToDistinctUntilChanged()
        {
            // Arrange
            var source = new[] { "apple", "APPLE", "banana", "BANANA", "cherry" };
            var comparer = StringComparer.OrdinalIgnoreCase;
            var distinctResults = new List<string>();
            var distinctUntilChangedResults = new List<string>();

            // Act
            source.ToObservable()
                .Distinct(comparer)
                .Subscribe(value => distinctResults.Add(value));

            source.ToObservable()
                .DistinctUntilChanged(comparer)
                .Subscribe(value => distinctUntilChangedResults.Add(value));

            // Assert
            Assert.AreEqual(distinctUntilChangedResults.ToArray(), distinctResults.ToArray());
            Assert.AreEqual(new[] { "apple", "banana", "cherry" }, distinctResults.ToArray());
        }

        #endregion

        #region Void (AsUnitObservable) Tests

        [Test]
        public void Void_ConvertsToUnit_IdenticalToAsUnitObservable()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var voidResults = new List<Unit>();
            var asUnitResults = new List<Unit>();

            // Act
            source.ToObservable()
                .Void()
                .Subscribe(value => voidResults.Add(value));

            source.ToObservable()
                .AsUnitObservable()
                .Subscribe(value => asUnitResults.Add(value));

            // Assert
            Assert.AreEqual(asUnitResults.Count, voidResults.Count);
            Assert.AreEqual(3, voidResults.Count);
            Assert.IsTrue(voidResults.All(unit => unit == Unit.Default));
            Assert.IsTrue(asUnitResults.All(unit => unit == Unit.Default));
        }

        [Test]
        public void Void_WithComplexType_IdenticalToAsUnitObservable()
        {
            // Arrange
            var source = new[] { "test", "data", "values" };
            var voidResults = new List<Unit>();
            var asUnitResults = new List<Unit>();
            var voidCompleted = false;
            var asUnitCompleted = false;

            // Act
            source.ToObservable()
                .Void()
                .Subscribe(
                    value => voidResults.Add(value),
                    onCompleted: () => voidCompleted = true
                );

            source.ToObservable()
                .AsUnitObservable()
                .Subscribe(
                    value => asUnitResults.Add(value),
                    onCompleted: () => asUnitCompleted = true
                );

            // Assert
            Assert.AreEqual(asUnitResults.Count, voidResults.Count);
            Assert.AreEqual(voidCompleted, asUnitCompleted);
            Assert.IsTrue(voidCompleted);
            Assert.AreEqual(3, voidResults.Count);
        }

        [Test]
        public void Debounce_AsynchronousTest_WorksCorrectly()
        {
            // Arrange
            var results = new List<Unit>();
            var sideEffects = new List<string>();

            // Act - Test Debounce with asynchronous behavior
            // Create a subject to control timing
            var subject = new Subject<int>();

            subject
                .Map(x => x % 3)                                    // Transform values
                .Distinct()                                         // Remove consecutive duplicates
                .Tap(x => sideEffects.Add($"Value: {x}"))           // Side effects
                .Debounce(TimeSpan.FromMilliseconds(50))            // Debounce with reasonable delay
                .Void()                                             // Convert to Unit
                .Subscribe(unit => results.Add(unit));

            // Emit values with delays to test debounce behavior
            subject.OnNext(1);
            subject.OnNext(2);
            subject.OnNext(0);
            subject.OnCompleted();

            // Wait a bit for debounce to complete
            System.Threading.Thread.Sleep(100);

            // Assert - Debounce should emit only the last value (0) after the delay
            Assert.AreEqual(1, results.Count); // Only final debounced value
            Assert.IsTrue(results.All(unit => unit == Unit.Default));
            Assert.AreEqual(3, sideEffects.Count); // All values should be processed by Tap
            Assert.Contains("Value: 1", sideEffects);
            Assert.Contains("Value: 2", sideEffects);
            Assert.Contains("Value: 0", sideEffects);
        }

        [Test]
        public void Reduce_BehavesLikeAggregate_EmitsOnlyFinalResult()
        {
            // Arrange
            var source = new[] { 6, 8 };
            var reduceResults = new List<int>();
            var aggregateResults = new List<int>();

            // Act - Test both Reduce and explicit Reactive Aggregate
            source.ToObservable()
                .Reduce((acc, x) => acc + x)
                .Subscribe(value => reduceResults.Add(value));

            // Use explicit extension method to avoid LINQ conflict
            ObservableExtensions.Aggregate(source.ToObservable(), (acc, x) => acc + x)
                .Subscribe(value => aggregateResults.Add(value));

            // Assert - Both should behave identically
            Assert.AreEqual(aggregateResults.Count, reduceResults.Count);
            Assert.AreEqual(1, reduceResults.Count); // Should emit only final result
            Assert.AreEqual(14, reduceResults[0]); // 6 + 8 = 14
            Assert.AreEqual(aggregateResults[0], reduceResults[0]);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void UniversalTerminology_ChainedOperations_WorksCorrectly()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var results = new List<string>();

            // Act - Chain Map, FlatMap, and Fold together
            source.ToObservable()
                .Map(x => x * 2)                                    // Transform: 2, 4, 6
                .FlatMap(x => Observable.Range(x, 2))               // Flatten: 2,3, 4,5, 6,7
                .Fold("", (acc, x) => acc + x.ToString())           // Accumulate: "2", "23", "234", "2345", "23456", "234567"
                .Subscribe(value => results.Add(value));

            // Assert
            Assert.AreEqual(6, results.Count);
            Assert.AreEqual("2", results[0]);
            Assert.AreEqual("23", results[1]);
            Assert.AreEqual("234", results[2]);
            Assert.AreEqual("2345", results[3]);
            Assert.AreEqual("23456", results[4]);
            Assert.AreEqual("234567", results[5]);
        }

        [Test]
        public void UniversalTerminology_AllPhase1Aliases_WorksCorrectly()
        {
            // Arrange
            var results = new List<string>();
            var sideEffects = new List<int>();

            // Act - Use all Phase 1 universal terminology aliases together
            Observable.Of(10)                                       // Create single value
                .Filter(x => x > 5)                                 // Filter values
                .Map(x => x * 2)                                    // Transform: 20
                .Tap(x => sideEffects.Add(x))                       // Side effect: record 20
                .FlatMap(x => Observable.Range(1, 3))               // Flatten: 1, 2, 3
                .Reduce(0, (acc, x) => acc + x, acc => $"Total: {acc}") // Reduce: "Total: 6"
                .Subscribe(value => results.Add(value));

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Total: 6", results[0]);
            Assert.AreEqual(1, sideEffects.Count);
            Assert.AreEqual(20, sideEffects[0]);
        }

        [Test]
        public void UniversalTerminology_AllPhase2Aliases_WorksCorrectly()
        {
            // Arrange
            var results = new List<Unit>();
            var sideEffects = new List<string>();

            // Act - Use all Phase 2 universal terminology aliases together
            // Note: Debounce is asynchronous, so we test without it for synchronous behavior
            Observable.Range(1, 6)                                  // Create sequence: 1,2,3,4,5,6
                .Map(x => x % 3)                                    // Transform: 1,2,0,1,2,0
                .Distinct()                                         // Remove consecutive duplicates: 1,2,0,1,2,0 (no consecutive duplicates)
                .Tap(x => sideEffects.Add($"Value: {x}"))           // Side effects
                .Void()                                             // Convert to Unit
                .Subscribe(unit => results.Add(unit));

            // Assert
            Assert.AreEqual(6, results.Count); // All values should pass through
            Assert.IsTrue(results.All(unit => unit == Unit.Default));
            Assert.AreEqual(6, sideEffects.Count);
            Assert.Contains("Value: 1", sideEffects);
            Assert.Contains("Value: 2", sideEffects);
            Assert.Contains("Value: 0", sideEffects);
        }

        [Test]
        public void UniversalTerminology_AllAliasesCombined_WorksCorrectly()
        {
            // Arrange
            var results = new List<Unit>();
            var sideEffects = new List<string>();

            // Act - Use all universal terminology aliases (Phase 1 + Phase 2) together
            // Note: Debounce is asynchronous, so we test without it for synchronous behavior
            Observable.Of(5)                                        // Phase 1: Of
                .Filter(x => x > 0)                                 // Phase 1: Filter
                .Map(x => x * 2)                                    // Phase 1: Map (10)
                .FlatMap(x => Observable.Range(x, 3))               // Phase 1: FlatMap (10,11,12)
                .Distinct()                                         // Phase 2: Distinct (10,11,12 - no duplicates)
                .Tap(x => sideEffects.Add($"Processing: {x}"))      // Phase 1: Tap
                .Fold(0, (acc, x) => acc + x)                       // Phase 1: Fold (0,10,21,33)
                .Reduce((acc, x) => Math.Max(acc, x))               // Phase 1: Reduce (final max: 33)
                .Void()                                             // Phase 2: Void
                .Subscribe(unit => results.Add(unit));

            // Assert
            Assert.AreEqual(1, results.Count); // Final result should be single Unit
            Assert.AreEqual(Unit.Default, results[0]);
            Assert.AreEqual(3, sideEffects.Count); // Should process 10, 11, 12
            Assert.Contains("Processing: 10", sideEffects);
            Assert.Contains("Processing: 11", sideEffects);
            Assert.Contains("Processing: 12", sideEffects);
        }

        [Test]
        public void UniversalTerminology_MixedWithOriginalMethods_WorksCorrectly()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5 };
            var results = new List<int>();

            // Act - Mix universal terminology with original methods
            // Simplified version to avoid potential issues with FlatMap
            var observable = source.ToObservable()
                .Map(x => x * 2)                    // Universal: Map (2, 4, 6, 8, 10)
                .Filter(x => x > 4)                 // Universal: Filter (6, 8, 10)
                .Take(2);                           // Original: Take (6, 8)

            // Use explicit extension method to avoid LINQ conflict
            ObservableExtensions.Reduce(observable, (acc, x) => acc + x)
                .Subscribe(value => results.Add(value));

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(14, results[0]); // 6 + 8 = 14
        }

        [Test]
        public void UniversalTerminology_ComprehensiveMixedUsage_WorksCorrectly()
        {
            // Arrange
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var results = new List<string>();
            var sideEffects = new List<string>();

            // Act - Comprehensive test mixing all terminologies
            source.ToObservable()
                .Filter(x => x % 2 == 0)                            // Universal: Filter (even numbers: 2, 4, 6)
                .Map(x => x * 10)                                   // Universal: Map (20, 40, 60)
                .Tap(x => sideEffects.Add($"Processing: {x}"))      // Universal: Tap (side effects)
                .Where(x => x > 25)                                 // Original: Where (40, 60)
                .FlatMap(x => Observable.Of($"Value-{x}"))          // Universal: FlatMap + Of
                .Do(x => sideEffects.Add($"Final: {x}"))            // Original: Do (more side effects)
                .Fold("", (acc, x) => acc + x + ";")                // Universal: Fold
                .Subscribe(value => results.Add(value));

            // Assert
            Assert.AreEqual(2, results.Count); // Fold emits 2 accumulated values (seed not emitted)
            Assert.AreEqual("Value-40;", results[0]);
            Assert.AreEqual("Value-40;Value-60;", results[1]);
            Assert.AreEqual(5, sideEffects.Count); // 3 from Tap + 2 from Do
            Assert.Contains("Processing: 20", sideEffects);
            Assert.Contains("Processing: 40", sideEffects);
            Assert.Contains("Processing: 60", sideEffects);
            Assert.Contains("Final: Value-40", sideEffects);
            Assert.Contains("Final: Value-60", sideEffects);
        }

        #endregion
    }
}