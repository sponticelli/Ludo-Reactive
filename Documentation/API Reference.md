# Ludo.Reactive - API Reference

## Table of Contents
1. [Core Interfaces](#core-interfaces)
2. [Observable Creation](#observable-creation)
3. [Reactive Properties](#reactive-properties)
4. [Subjects](#subjects)
5. [Operators](#operators)
6. [Unity Integration](#unity-integration)
7. [Schedulers](#schedulers)
8. [Collections](#collections)
9. [Error Handling](#error-handling)
10. [Async Support](#async-support)

## Core Interfaces

### IObservable<T>
Defines a provider for push-based notification.

```csharp
public interface IObservable<out T>
{
    IDisposable Subscribe(IObserver<T> observer);
}
```

### IObserver<T>
Provides a mechanism for receiving push-based notifications.

```csharp
public interface IObserver<in T>
{
    void OnNext(T value);
    void OnError(Exception error);
    void OnCompleted();
}
```

### IReadOnlyReactiveProperty<T>
Read-only interface for reactive properties.

```csharp
public interface IReadOnlyReactiveProperty<out T> : IObservable<T>
{
    T Value { get; }
}
```

## Observable Creation

### Observable.Return<T>(T value)
Returns an observable sequence that contains a single element.

**Parameters:**
- `value`: Single element in the resulting observable sequence

**Returns:** An observable sequence containing the single specified element

### Observable.Empty<T>()
Returns an empty observable sequence.

**Returns:** An observable sequence with no elements

### Observable.Never<T>()
Returns a non-terminating observable sequence.

**Returns:** An observable sequence whose observers will never get called

### Observable.Throw<T>(Exception exception)
Returns an observable sequence that terminates with an exception.

**Parameters:**
- `exception`: Exception object used for the sequence's termination

### Observable.Create<T>(Func<IObserver<T>, IDisposable> subscribe)
Creates an observable sequence from a specified Subscribe method implementation.

**Parameters:**
- `subscribe`: Implementation of the resulting observable sequence's Subscribe method

### Observable.Interval(TimeSpan period)
Returns an observable sequence that produces a value after each period.

**Parameters:**
- `period`: Period for producing the values

### Observable.Timer(TimeSpan dueTime)
Returns an observable sequence that produces a value after the due time has elapsed.

**Parameters:**
- `dueTime`: Relative time at which to produce the value

### Observable.Generate<TState, TResult>(...)
Generates an observable sequence by running a state-driven loop.

**Parameters:**
- `initialState`: Initial state
- `condition`: Condition to terminate generation
- `iterate`: Iteration step function
- `resultSelector`: Selector function for results

### Observable.Range(int start, int count)
Generates an observable sequence of integral numbers within a specified range.

**Parameters:**
- `start`: The value of the first integer in the sequence
- `count`: The number of sequential integers to generate

### Observable.FromCoroutine(Func<IEnumerator> coroutine)
Converts a Unity coroutine to an observable sequence.

**Parameters:**
- `coroutine`: The coroutine to convert

### Observable.FromCoroutine<T>(Func<IObserver<T>, IEnumerator> coroutine)
Converts a coroutine that yields values to an observable sequence.

**Parameters:**
- `coroutine`: The coroutine to convert

### Observable.FromUnityEvent(UnityEvent unityEvent)
Converts a UnityEvent to an observable sequence.

**Parameters:**
- `unityEvent`: The UnityEvent to convert

### Observable.FromUnityEvent<T>(UnityEvent<T> unityEvent)
Converts a UnityEvent<T> to an observable sequence.

**Parameters:**
- `unityEvent`: The UnityEvent to convert

### Observable.FromAsyncOperation<T>(T asyncOperation)
Converts an AsyncOperation to an observable sequence.

**Parameters:**
- `asyncOperation`: The AsyncOperation to convert

## Reactive Properties

### ReactiveProperty<T>
A mutable container that notifies observers when its value changes.

```csharp
public class ReactiveProperty<T> : IObservable<T>, IDisposable
{
    public T Value { get; set; }
    public IReadOnlyReactiveProperty<T> AsReadOnly();
    public IDisposable Subscribe(IObserver<T> observer);
    public void Dispose();
}
```

**Constructors:**
- `ReactiveProperty()`: Initializes with default value
- `ReactiveProperty(T initialValue)`: Initializes with specified value
- `ReactiveProperty(T initialValue, IEqualityComparer<T> comparer)`: Initializes with value and comparer

**Properties:**
- `Value`: Gets or sets the current value (triggers notifications on change)

**Methods:**
- `AsReadOnly()`: Returns a read-only view of the property
- `Subscribe(IObserver<T>)`: Subscribes an observer (immediately receives current value)
- `Dispose()`: Releases all resources

## Subjects

### Subject<T>
Represents an object that is both an observable sequence and an observer.

```csharp
public sealed class Subject<T> : ISubject<T>, IDisposable
{
    public void OnNext(T value);
    public void OnError(Exception error);
    public void OnCompleted();
    public IDisposable Subscribe(IObserver<T> observer);
    public void Dispose();
}
```

### BehaviorSubject<T>
Represents a value that changes over time. Observers receive the last value and all subsequent notifications.

```csharp
public sealed class BehaviorSubject<T> : ISubject<T>, IDisposable
{
    public T Value { get; }
    // Same methods as Subject<T>
}
```

### ReplaySubject<T>
Represents a subject that buffers and replays multiple values to new subscribers.

```csharp
public sealed class ReplaySubject<T> : ISubject<T>, IDisposable
{
    // Constructors with buffer size and time window options
    // Same methods as Subject<T>
}
```

## Operators

### Transformation Operators

#### Select<TSource, TResult>(Func<TSource, TResult> selector)
Projects each element of an observable sequence into a new form.

**Parameters:**
- `selector`: A transform function to apply to each source element

#### SelectMany<TSource, TResult>(Func<TSource, IObservable<TResult>> selector)
Projects each element to an observable sequence and flattens the resulting sequences.

**Parameters:**
- `selector`: A transform function to apply to each element

#### Buffer(TimeSpan timeSpan)
Projects elements into zero or more buffers based on time information.

**Parameters:**
- `timeSpan`: Maximum time length of a buffer

#### Buffer(int count, int skip)
Projects elements into zero or more buffers based on element count.

**Parameters:**
- `count`: Maximum element count of a buffer
- `skip`: Number of elements to skip between creation of consecutive buffers

#### Scan<T, TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> accumulator)
Applies an accumulator function over an observable sequence and returns each intermediate result.

**Parameters:**
- `seed`: The initial accumulator value
- `accumulator`: An accumulator function to be invoked on each element

### Universal Reactive Programming Terminology Aliases

These aliases provide familiar terminology for developers from other reactive programming ecosystems (RxJS, RxJava, etc.) while maintaining 100% backward compatibility.

#### Map<TSource, TResult>(Func<TSource, TResult> selector)
**Alias for Select()** - Projects each element of an observable sequence into a new form.

**Parameters:**
- `selector`: A transform function to apply to each source element

**Note:** Identical functionality to `Select()`. Use whichever terminology you prefer.

#### FlatMap<TSource, TResult>(Func<TSource, IObservable<TResult>> selector)
**Alias for SelectMany()** - Projects each element to an observable sequence and flattens the resulting sequences.

**Parameters:**
- `selector`: A transform function to apply to each element

**Note:** Identical functionality to `SelectMany()`. Use whichever terminology you prefer.

#### Fold<TSource, TAccumulate>(TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
**Alias for Scan()** - Applies an accumulator function over an observable sequence and returns each intermediate result.

**Parameters:**
- `seed`: The initial accumulator value
- `accumulator`: An accumulator function to be invoked on each element

**Note:** Identical functionality to `Scan()`. Use whichever terminology you prefer.

#### Filter<T>(Func<T, bool> predicate)
**Alias for Where()** - Filters the elements of an observable sequence based on a predicate.

**Parameters:**
- `predicate`: A function to test each source element for a condition

**Note:** Identical functionality to `Where()`. Standard terminology in RxJS, RxJava, RxSwift, and functional programming.

#### Tap<T>(Action<T> onNext)
**Alias for Do()** - Invokes a specified action for each element in the observable sequence.

**Parameters:**
- `onNext`: Action to invoke for each element in the observable sequence

**Note:** Identical functionality to `Do()`. Standard terminology in RxJS, RxJava, RxSwift, and Angular.

#### Reduce<TSource, TAccumulate, TResult>(TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator, Func<TAccumulate, TResult> resultSelector)
**Alias for Aggregate()** - Applies an accumulator function over an observable sequence and returns the final result.

**Parameters:**
- `seed`: The initial accumulator value
- `accumulator`: An accumulator function to be invoked on each element
- `resultSelector`: A function to transform the final accumulator value into the result value

**Note:** Identical functionality to `Aggregate()`. Standard terminology in RxJS, JavaScript, and functional programming.

#### Of<T>(T value)
**Alias for Return()** - Returns an observable sequence that contains a single element.

**Parameters:**
- `value`: Single element in the resulting observable sequence

**Note:** Identical functionality to `Return()`. Standard terminology in RxJS, RxJava, and RxSwift.

#### Debounce<T>(TimeSpan dueTime)
**Alias for Throttle()** - Returns elements only after the specified duration has passed without another value being emitted.

**Parameters:**
- `dueTime`: Debouncing duration for each element

**Note:** Identical functionality to `Throttle()`. Standard terminology in RxJS, web development, and Angular.

#### Distinct<T>()
**Alias for DistinctUntilChanged()** - Returns only distinct contiguous elements according to the default equality comparer.

**Note:** Identical functionality to `DistinctUntilChanged()`. Simplified terminology from RxJava and functional programming.

#### Void<T>()
**Alias for AsUnitObservable()** - Converts an observable sequence to a Unit observable sequence.

**Note:** Identical functionality to `AsUnitObservable()`. Standard terminology in RxSwift and RxJava for discarding values.

### Filtering Operators

#### Where<T>(Func<T, bool> predicate)
Filters the elements of an observable sequence based on a predicate.

**Parameters:**
- `predicate`: A function to test each source element for a condition

#### DistinctUntilChanged<T>()
Returns an observable sequence that contains only distinct contiguous elements.

#### Take<T>(int count)
Returns a specified number of contiguous elements from the start of an observable sequence.

**Parameters:**
- `count`: The number of elements to return

#### TakeUntil<T, TOther>(IObservable<TOther> other)
Returns elements from the source until the other observable emits a value.

**Parameters:**
- `other`: Observable sequence that terminates propagation of elements

#### Throttle<T>(TimeSpan dueTime)
Ignores values from the source for the specified duration after each value.

**Parameters:**
- `dueTime`: Duration to ignore values after each emission

### Combination Operators

#### CombineLatest<TFirst, TSecond, TResult>(IObservable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
Merges two observable sequences into one by using the latest values.

**Parameters:**
- `second`: Second observable source
- `resultSelector`: Function to invoke when either source emits

#### Merge<T>(params IObservable<T>[] sources)
Merges multiple observable sequences into a single observable sequence.

**Parameters:**
- `sources`: Observable sequences to merge

#### Zip<TFirst, TSecond, TResult>(IObservable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
Merges two observable sequences into one by pairing elements at corresponding indices.

**Parameters:**
- `second`: Second observable source
- `resultSelector`: Function to invoke for each pair of elements at the same index

**Returns:** An observable sequence containing the result of pairwise combining elements

**Behavior:** Emits when both sources have emitted at the same index. Completes when either source completes.

### Utility Operators

#### Do<T>(Action<T> onNext)
Invokes a specified action for each element in the observable sequence.

**Parameters:**
- `onNext`: Action to invoke for each element

#### Delay<T>(TimeSpan dueTime)
Time-shifts the observable sequence by the specified duration.

**Parameters:**
- `dueTime`: Relative time by which to shift the observable sequence

#### Timeout<T>(TimeSpan dueTime)
Applies a timeout policy for each element in the observable sequence.

**Parameters:**
- `dueTime`: Maximum duration between values before a timeout occurs

### Aggregation Operators

#### Aggregate<TSource, TAccumulate, TResult>(TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator, Func<TAccumulate, TResult> resultSelector)
Applies an accumulator function over an observable sequence and returns the final result.

**Parameters:**
- `seed`: The initial accumulator value
- `accumulator`: An accumulator function to be invoked on each element
- `resultSelector`: A function to transform the final accumulator value

### Conversion Operators

#### Cast<TSource, TResult>()
Converts the elements of an observable sequence to the specified type.

#### OfType<TSource, TResult>()
Filters the elements of an observable sequence based on a specified type.

#### AsUnitObservable<T>()
Converts an observable sequence to a Unit observable sequence.

### Multicasting Operators

#### Publish<T>()
Returns a connectable observable sequence that shares a single subscription.

#### Replay<T>()
Returns a connectable observable sequence that shares a single subscription and replays all notifications.

#### Replay<T>(int bufferSize)
Returns a connectable observable sequence with a specified replay buffer size.

#### RefCount<T>()
Returns an observable sequence that stays connected as long as there is at least one subscription.

### Scheduler Operators

#### ObserveOn<T>(IScheduler scheduler)
Wraps the source sequence to notify observers on the specified scheduler.

**Parameters:**
- `scheduler`: Scheduler to notify observers on

#### SubscribeOn<T>(IScheduler scheduler)
Wraps the source sequence to subscribe and unsubscribe on the specified scheduler.

**Parameters:**
- `scheduler`: Scheduler to perform subscription actions on

### Error Handling Operators

#### Catch<T>(Func<Exception, IObservable<T>> handler)
Continues an observable sequence that is terminated by an exception with the next observable sequence.

**Parameters:**
- `handler`: Exception handler function producing another observable sequence

#### Catch<T, TException>(Func<TException, IObservable<T>> handler)
Continues an observable sequence that is terminated by a specific exception type.

**Parameters:**
- `handler`: Exception handler function for the specific exception type

#### LogError<T>(string context = null)
Logs errors using Unity's Debug.LogError without affecting the stream.

**Parameters:**
- `context`: Optional context string for the error message

## Unity Integration

### Extension Methods

#### AddTo<T>(Component component)
Automatically disposes the subscription when the specified Component is destroyed.

**Parameters:**
- `component`: The component whose lifetime will control the disposal

### Lifecycle Observables

#### OnDestroyAsObservable()
Observes when a GameObject is destroyed.

#### OnEnableAsObservable()
Observes when a GameObject is enabled.

#### OnDisableAsObservable()
Observes when a GameObject is disabled.

#### UpdateAsObservable()
Observes Unity's Update loop.

#### FixedUpdateAsObservable()
Observes Unity's FixedUpdate loop.

#### LateUpdateAsObservable()
Observes Unity's LateUpdate loop.

#### EveryUpdate()
Creates a global observable that emits every frame during Update.

#### EveryFixedUpdate()
Creates a global observable that emits every fixed frame during FixedUpdate.

#### EveryLateUpdate()
Creates a global observable that emits every frame during LateUpdate.

### UI Event Extensions

#### OnClickAsObservable() (Button)
Converts a Button's onClick event to an observable sequence.

#### OnValueChangedAsObservable() (Toggle)
Converts a Toggle's onValueChanged event to an observable sequence.

#### OnValueChangedAsObservable() (Slider)
Converts a Slider's onValueChanged event to an observable sequence.

#### OnValueChangedAsObservable() (InputField)
Converts an InputField's onValueChanged event to an observable sequence.

#### OnEndEditAsObservable() (InputField)
Converts an InputField's onEndEdit event to an observable sequence.

#### OnValueChangedAsObservable() (Dropdown)
Converts a Dropdown's onValueChanged event to an observable sequence.

## Schedulers

### UnitySchedulers
Provides Unity-specific schedulers for different execution contexts.

#### UnitySchedulers.MainThread
Scheduler that executes work on Unity's main thread.

#### UnitySchedulers.FixedUpdate
Scheduler that executes work during Unity's FixedUpdate.

#### UnitySchedulers.LateUpdate
Scheduler that executes work during Unity's LateUpdate.

#### UnitySchedulers.EndOfFrame
Scheduler that executes work at the end of the frame.

### IScheduler Interface

```csharp
public interface IScheduler
{
    DateTimeOffset Now { get; }
    IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action);
    IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action);
}
```

## Collections

### ReactiveCollection<T>
Represents a collection that provides notifications when items get added, removed, or when the whole list is refreshed.

```csharp
public class ReactiveCollection<T> : IList<T>, INotifyCollectionChanged, IDisposable
{
    // Collection operations
    public IObservable<CollectionAddEvent<T>> ObserveAdd();
    public IObservable<CollectionRemoveEvent<T>> ObserveRemove();
    public IObservable<CollectionReplaceEvent<T>> ObserveReplace();
    public IObservable<CollectionMoveEvent<T>> ObserveMove();
    public IObservable<CollectionResetEvent<T>> ObserveReset();
    public IObservable<int> ObserveCountChanged();
}
```

### Collection Events

#### CollectionAddEvent<T>
Represents an event when an item is added to a reactive collection.

**Properties:**
- `Index`: The index where the item was added
- `Value`: The item that was added

#### CollectionRemoveEvent<T>
Represents an event when an item is removed from a reactive collection.

**Properties:**
- `Index`: The index where the item was removed
- `Value`: The item that was removed

#### CollectionReplaceEvent<T>
Represents an event when an item is replaced in a reactive collection.

**Properties:**
- `Index`: The index where the item was replaced
- `OldValue`: The old item value
- `NewValue`: The new item value

#### CollectionMoveEvent<T>
Represents an event when an item is moved in a reactive collection.

**Properties:**
- `OldIndex`: The old index of the item
- `NewIndex`: The new index of the item
- `Value`: The item that was moved

#### CollectionResetEvent<T>
Represents an event when a reactive collection is reset (cleared).

**Properties:**
- `OldItems`: The items that were in the collection before it was reset

## Error Handling

### Exception Types

#### TimeoutException
Thrown when an operation times out.

### Error Handling Patterns

```csharp
// Basic error handling
observable
    .Catch(ex => Observable.Return(defaultValue))
    .Subscribe(value => ProcessValue(value));

// Specific exception handling
observable
    .Catch<MyException>(ex => HandleSpecificError(ex))
    .Subscribe(value => ProcessValue(value));

// Error logging
observable
    .LogError("Context information")
    .Subscribe(value => ProcessValue(value));
```

## Async Support

### Task Integration

#### ToTask<T>()
Converts an observable sequence to a Task that completes with the first emitted value.

#### ToArrayAsync<T>()
Converts an observable sequence to a Task<T[]> that completes with all emitted values.

#### ToObservable<T>() (Task<T>)
Converts a Task<T> to an observable sequence.

### AsyncOperation Integration

#### ToObservable<T>() (AsyncOperation)
Converts an AsyncOperation to an observable sequence that emits when the operation completes.

#### ToObservableWithProgress() (AsyncOperation)
Converts an AsyncOperation to an observable sequence that tracks progress.

### Usage Examples

```csharp
// Convert observable to task
var result = await observable.ToTask();

// Convert task to observable
var observable = task.ToObservable();

// Track async operation progress
SceneManager.LoadSceneAsync("MyScene")
    .ToObservableWithProgress()
    .Subscribe(progress => UpdateProgressBar(progress));
```

## Memory Management

### Disposable Utilities

#### CompositeDisposable
Manages multiple disposables as a group.

```csharp
var composite = new CompositeDisposable();
composite.Add(subscription1);
composite.Add(subscription2);
// Dispose all at once
composite.Dispose();
```

#### Disposable.Create(Action dispose)
Creates a custom disposable from an action.

#### Disposable.Empty
A no-op disposable that does nothing when disposed.

### Best Practices

1. Always use `.AddTo(this)` for automatic disposal with GameObject lifecycle
2. Use `CompositeDisposable` for manual subscription management
3. Dispose subscriptions when no longer needed
4. Use read-only properties to prevent external modification
5. Leverage operators for data transformation instead of manual state management

---

For more detailed examples and usage patterns, see the [Examples](Examples.md) documentation.
