# Ludo.Reactive - Testing Guide

## Table of Contents
1. [Testing Overview](#testing-overview)
2. [Unit Testing Reactive Code](#unit-testing-reactive-code)
3. [Testing Reactive Properties](#testing-reactive-properties)
4. [Testing Observable Sequences](#testing-observable-sequences)
5. [Testing Unity Integration](#testing-unity-integration)
6. [Async Testing](#async-testing)
7. [Testing Patterns](#testing-patterns)
8. [Common Testing Scenarios](#common-testing-scenarios)

## Testing Overview

Testing reactive code requires understanding the asynchronous and event-driven nature of observables. Ludo.Reactive provides several patterns and utilities to make testing easier and more reliable.

### Key Testing Principles

1. **Deterministic Testing**: Use controlled timing and predictable sequences
2. **Isolation**: Test individual components without external dependencies
3. **Verification**: Assert both values and timing of emissions
4. **Cleanup**: Properly dispose of subscriptions in tests

## Unit Testing Reactive Code

### Basic Test Setup

```csharp
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Ludo.Reactive;

[TestFixture]
public class ReactiveTests
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
}
```

### Testing Observable Emissions

```csharp
[Test]
public void TestObservableEmissions()
{
    var values = new List<int>();
    var completed = false;
    
    var observable = Observable.Create<int>(observer =>
    {
        observer.OnNext(1);
        observer.OnNext(2);
        observer.OnNext(3);
        observer.OnCompleted();
        return Disposable.Empty;
    });
    
    AddDisposable(observable.Subscribe(
        value => values.Add(value),
        () => completed = true
    ));
    
    Assert.AreEqual(new[] { 1, 2, 3 }, values);
    Assert.IsTrue(completed);
}
```

### Testing Error Handling

```csharp
[Test]
public void TestErrorHandling()
{
    var exception = new InvalidOperationException("Test error");
    var errorReceived = false;
    Exception receivedException = null;
    
    var observable = Observable.Throw<int>(exception);
    
    AddDisposable(observable.Subscribe(
        value => { },
        error => 
        {
            errorReceived = true;
            receivedException = error;
        }
    ));
    
    Assert.IsTrue(errorReceived);
    Assert.AreEqual(exception, receivedException);
}
```

## Testing Reactive Properties

### Basic ReactiveProperty Testing

```csharp
[Test]
public void TestReactivePropertyBasics()
{
    var property = new ReactiveProperty<int>(10);
    var values = new List<int>();
    
    AddDisposable(property.Subscribe(values.Add));
    
    // Should receive initial value
    Assert.AreEqual(new[] { 10 }, values);
    
    // Should receive new values
    property.Value = 20;
    property.Value = 30;
    
    Assert.AreEqual(new[] { 10, 20, 30 }, values);
}
```

### Testing Value Change Notifications

```csharp
[Test]
public void TestValueChangeNotifications()
{
    var property = new ReactiveProperty<string>("initial");
    var changeCount = 0;
    
    AddDisposable(property.Subscribe(_ => changeCount++));
    
    // Initial subscription should trigger once
    Assert.AreEqual(1, changeCount);
    
    // Same value should not trigger
    property.Value = "initial";
    Assert.AreEqual(1, changeCount);
    
    // Different value should trigger
    property.Value = "changed";
    Assert.AreEqual(2, changeCount);
}
```

### Testing Read-Only Properties

```csharp
[Test]
public void TestReadOnlyProperty()
{
    var property = new ReactiveProperty<int>(42);
    var readOnly = property.AsReadOnly();
    var values = new List<int>();
    
    AddDisposable(readOnly.Subscribe(values.Add));
    
    // Should receive initial value
    Assert.AreEqual(42, readOnly.Value);
    Assert.AreEqual(new[] { 42 }, values);
    
    // Changes to original should propagate
    property.Value = 100;
    Assert.AreEqual(100, readOnly.Value);
    Assert.AreEqual(new[] { 42, 100 }, values);
}
```

## Testing Observable Sequences

### Testing Operators

```csharp
[Test]
public void TestSelectOperator()
{
    var source = new Subject<int>();
    var results = new List<string>();
    
    AddDisposable(source
        .Select(x => $"Value: {x}")
        .Subscribe(results.Add));
    
    source.OnNext(1);
    source.OnNext(2);
    source.OnCompleted();
    
    Assert.AreEqual(new[] { "Value: 1", "Value: 2" }, results);
}

[Test]
public void TestWhereOperator()
{
    var source = new Subject<int>();
    var results = new List<int>();
    
    AddDisposable(source
        .Where(x => x % 2 == 0)
        .Subscribe(results.Add));
    
    source.OnNext(1);
    source.OnNext(2);
    source.OnNext(3);
    source.OnNext(4);
    source.OnCompleted();
    
    Assert.AreEqual(new[] { 2, 4 }, results);
}

[Test]
public void TestDistinctUntilChanged()
{
    var source = new Subject<int>();
    var results = new List<int>();
    
    AddDisposable(source
        .DistinctUntilChanged()
        .Subscribe(results.Add));
    
    source.OnNext(1);
    source.OnNext(1); // Duplicate
    source.OnNext(2);
    source.OnNext(2); // Duplicate
    source.OnNext(1); // Different from previous
    
    Assert.AreEqual(new[] { 1, 2, 1 }, results);
}

[Test]
public void TestZip()
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
```

### Testing Combination Operators

```csharp
[Test]
public void TestCombineLatest()
{
    var source1 = new Subject<int>();
    var source2 = new Subject<string>();
    var results = new List<string>();
    
    AddDisposable(source1
        .CombineLatest(source2, (i, s) => $"{i}-{s}")
        .Subscribe(results.Add));
    
    source1.OnNext(1);
    // No emission yet - need both sources
    Assert.AreEqual(0, results.Count);
    
    source2.OnNext("A");
    // Now we have both values
    Assert.AreEqual(new[] { "1-A" }, results);
    
    source1.OnNext(2);
    Assert.AreEqual(new[] { "1-A", "2-A" }, results);
    
    source2.OnNext("B");
    Assert.AreEqual(new[] { "1-A", "2-A", "2-B" }, results);
}
```

### Testing Time-Based Operators

```csharp
[Test]
public void TestTakeOperator()
{
    var source = new Subject<int>();
    var results = new List<int>();
    var completed = false;
    
    AddDisposable(source
        .Take(3)
        .Subscribe(
            results.Add,
            () => completed = true
        ));
    
    source.OnNext(1);
    source.OnNext(2);
    source.OnNext(3);
    
    Assert.IsTrue(completed);
    Assert.AreEqual(new[] { 1, 2, 3 }, results);
    
    // Further emissions should be ignored
    source.OnNext(4);
    Assert.AreEqual(new[] { 1, 2, 3 }, results);
}

[Test]
public void TestTakeUntil()
{
    var source = new Subject<int>();
    var trigger = new Subject<Unit>();
    var results = new List<int>();
    var completed = false;
    
    AddDisposable(source
        .TakeUntil(trigger)
        .Subscribe(
            results.Add,
            () => completed = true
        ));
    
    source.OnNext(1);
    source.OnNext(2);
    
    // Trigger should complete the sequence
    trigger.OnNext(Unit.Default);
    Assert.IsTrue(completed);
    
    // Further emissions should be ignored
    source.OnNext(3);
    Assert.AreEqual(new[] { 1, 2 }, results);
}
```

## Testing Unity Integration

### Testing Unity Lifecycle Integration

```csharp
[UnityTest]
public IEnumerator TestGameObjectLifecycle()
{
    var gameObject = new GameObject("Test");
    var component = gameObject.AddComponent<TestComponent>();
    var disposed = false;
    
    // Create a subscription that should be disposed when GameObject is destroyed
    Observable.Return(42)
        .Subscribe(_ => { })
        .AddTo(component);
    
    // Add disposal tracking
    component.OnDestroy += () => disposed = true;
    
    // Destroy the GameObject
    UnityEngine.Object.DestroyImmediate(gameObject);
    
    yield return null; // Wait one frame
    
    Assert.IsTrue(disposed);
}

public class TestComponent : MonoBehaviour
{
    public event System.Action OnDestroy;
    
    void OnDestroy()
    {
        OnDestroy?.Invoke();
    }
}
```

### Testing UI Event Integration

```csharp
[Test]
public void TestButtonClickObservable()
{
    var button = new GameObject().AddComponent<UnityEngine.UI.Button>();
    var clickCount = 0;
    
    AddDisposable(button.OnClickAsObservable()
        .Subscribe(_ => clickCount++));
    
    // Simulate button clicks
    button.onClick.Invoke();
    button.onClick.Invoke();
    
    Assert.AreEqual(2, clickCount);
}
```

## Async Testing

### Testing Coroutine Integration

```csharp
[UnityTest]
public IEnumerator TestCoroutineObservable()
{
    var completed = false;
    var result = 0;
    
    AddDisposable(Observable.FromCoroutine(() => TestCoroutine())
        .Subscribe(_ => 
        {
            completed = true;
            result = 42;
        }));
    
    yield return new WaitUntil(() => completed);
    
    Assert.IsTrue(completed);
    Assert.AreEqual(42, result);
}

private IEnumerator TestCoroutine()
{
    yield return new WaitForSeconds(0.1f);
    // Coroutine logic here
}
```

### Testing Async Operations

```csharp
[UnityTest]
public IEnumerator TestAsyncOperation()
{
    var progress = new List<float>();
    var completed = false;
    
    // Create a mock async operation
    var asyncOp = new MockAsyncOperation();
    
    AddDisposable(asyncOp.ToObservableWithProgress()
        .Subscribe(
            p => progress.Add(p),
            () => completed = true
        ));
    
    // Simulate progress
    asyncOp.SimulateProgress(0.5f);
    yield return null;
    
    asyncOp.SimulateProgress(1.0f);
    asyncOp.Complete();
    yield return null;
    
    Assert.Contains(0.5f, progress);
    Assert.Contains(1.0f, progress);
    Assert.IsTrue(completed);
}

public class MockAsyncOperation : AsyncOperation
{
    public void SimulateProgress(float progress)
    {
        // Simulate progress update
    }
    
    public void Complete()
    {
        // Simulate completion
    }
}
```

## Testing Patterns

### Test Helper Methods

```csharp
public static class ReactiveTestHelpers
{
    public static List<T> CollectValues<T>(IObservable<T> observable)
    {
        var values = new List<T>();
        observable.Subscribe(values.Add);
        return values;
    }
    
    public static bool WaitForCompletion<T>(IObservable<T> observable, float timeoutSeconds = 1f)
    {
        var completed = false;
        observable.Subscribe(_ => { }, () => completed = true);
        
        var startTime = Time.time;
        while (!completed && Time.time - startTime < timeoutSeconds)
        {
            // Wait
        }
        
        return completed;
    }
    
    public static IObservable<T> CreateTestObservable<T>(params T[] values)
    {
        return Observable.Create<T>(observer =>
        {
            foreach (var value in values)
            {
                observer.OnNext(value);
            }
            observer.OnCompleted();
            return Disposable.Empty;
        });
    }
}
```

### Mock Objects for Testing

```csharp
public class MockReactiveProperty<T> : IReadOnlyReactiveProperty<T>
{
    private readonly Subject<T> _subject = new Subject<T>();
    private T _value;
    
    public T Value 
    { 
        get => _value;
        set
        {
            _value = value;
            _subject.OnNext(value);
        }
    }
    
    public IDisposable Subscribe(IObserver<T> observer)
    {
        observer.OnNext(_value); // Send current value immediately
        return _subject.Subscribe(observer);
    }
    
    public void Dispose()
    {
        _subject.Dispose();
    }
}
```

## Common Testing Scenarios

### Testing Game State Changes

```csharp
[Test]
public void TestGameStateTransitions()
{
    var gameState = new ReactiveProperty<GameState>(GameState.Menu);
    var stateChanges = new List<GameState>();
    
    AddDisposable(gameState
        .DistinctUntilChanged()
        .Subscribe(stateChanges.Add));
    
    // Test state transitions
    gameState.Value = GameState.Playing;
    gameState.Value = GameState.Paused;
    gameState.Value = GameState.Playing; // Resume
    gameState.Value = GameState.GameOver;
    
    var expected = new[] 
    { 
        GameState.Menu, 
        GameState.Playing, 
        GameState.Paused, 
        GameState.Playing, 
        GameState.GameOver 
    };
    
    Assert.AreEqual(expected, stateChanges);
}

public enum GameState { Menu, Playing, Paused, GameOver }
```

### Testing Health System

```csharp
[Test]
public void TestHealthSystem()
{
    var health = new ReactiveProperty<int>(100);
    var deathTriggered = false;
    var healthChanges = new List<int>();
    
    // Monitor all health changes
    AddDisposable(health.Subscribe(healthChanges.Add));
    
    // Monitor death event
    AddDisposable(health
        .Where(h => h <= 0)
        .Take(1)
        .Subscribe(_ => deathTriggered = true));
    
    // Test damage
    health.Value = 75;
    health.Value = 50;
    health.Value = 0; // Should trigger death
    
    Assert.IsTrue(deathTriggered);
    Assert.AreEqual(new[] { 100, 75, 50, 0 }, healthChanges);
}
```

### Testing Input Combinations

```csharp
[Test]
public void TestInputCombo()
{
    var inputStream = new Subject<KeyCode>();
    var comboTriggered = false;
    var targetCombo = new[] { KeyCode.A, KeyCode.B, KeyCode.C };
    
    AddDisposable(inputStream
        .Buffer(3, 1)
        .Where(buffer => buffer.SequenceEqual(targetCombo))
        .Subscribe(_ => comboTriggered = true));
    
    // Test correct sequence
    inputStream.OnNext(KeyCode.A);
    inputStream.OnNext(KeyCode.B);
    inputStream.OnNext(KeyCode.C);
    
    Assert.IsTrue(comboTriggered);
}

[Test]
public void TestInputComboFailure()
{
    var inputStream = new Subject<KeyCode>();
    var comboTriggered = false;
    var targetCombo = new[] { KeyCode.A, KeyCode.B, KeyCode.C };
    
    AddDisposable(inputStream
        .Buffer(3, 1)
        .Where(buffer => buffer.SequenceEqual(targetCombo))
        .Subscribe(_ => comboTriggered = true));
    
    // Test incorrect sequence
    inputStream.OnNext(KeyCode.A);
    inputStream.OnNext(KeyCode.X); // Wrong key
    inputStream.OnNext(KeyCode.C);
    
    Assert.IsFalse(comboTriggered);
}
```

### Testing Collection Changes

```csharp
[Test]
public void TestReactiveCollection()
{
    var collection = new ReactiveCollection<string>();
    var addEvents = new List<string>();
    var removeEvents = new List<string>();
    
    AddDisposable(collection.ObserveAdd()
        .Subscribe(e => addEvents.Add(e.Value)));
    
    AddDisposable(collection.ObserveRemove()
        .Subscribe(e => removeEvents.Add(e.Value)));
    
    // Test operations
    collection.Add("Item1");
    collection.Add("Item2");
    collection.Remove("Item1");
    
    Assert.AreEqual(new[] { "Item1", "Item2" }, addEvents);
    Assert.AreEqual(new[] { "Item1" }, removeEvents);
    Assert.AreEqual(1, collection.Count);
    Assert.Contains("Item2", collection);
}
```

### Performance Testing

```csharp
[Test]
public void TestPerformance()
{
    var source = new Subject<int>();
    var processedCount = 0;
    
    AddDisposable(source
        .Where(x => x % 2 == 0)
        .Select(x => x * 2)
        .Subscribe(_ => processedCount++));
    
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Send many values
    for (int i = 0; i < 10000; i++)
    {
        source.OnNext(i);
    }
    
    stopwatch.Stop();
    
    Assert.AreEqual(5000, processedCount); // Half should be even
    Assert.Less(stopwatch.ElapsedMilliseconds, 100); // Should be fast
}
```

## Testing Best Practices

1. **Always dispose subscriptions** in test teardown
2. **Use deterministic test data** instead of random values
3. **Test both success and error scenarios**
4. **Verify timing and order** of emissions
5. **Use helper methods** to reduce test boilerplate
6. **Mock external dependencies** for isolated testing
7. **Test edge cases** like empty sequences and immediate completion
8. **Verify subscription lifecycle** and proper disposal

---

Following these testing patterns will help ensure your reactive code is reliable and maintainable.
