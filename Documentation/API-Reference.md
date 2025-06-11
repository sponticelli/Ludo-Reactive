# API Reference

Complete reference for all public APIs in Ludo.Reactive.

## Core Interfaces

### IObservable

Base interface for all observable types.

```csharp
public interface IObservable
{
    SubscriptionHandle Subscribe(Action callback);
    void Unsubscribe(Action callback);
}
```

**Methods:**
- `Subscribe(Action callback)`: Subscribe to notifications. Returns a handle for unsubscription.
- `Unsubscribe(Action callback)`: Remove a subscription.

### IObservable<T>

Generic observable interface with typed notifications.

```csharp
public interface IObservable<T> : IObservable
{
    SubscriptionHandle Subscribe(Action<T> callback);
    void Unsubscribe(Action<T> callback);
}
```

**Methods:**
- `Subscribe(Action<T> callback)`: Subscribe with typed callback.
- `Unsubscribe(Action<T> callback)`: Remove typed subscription.

### IReadOnlyReactiveValue<T>

Read-only reactive value interface.

```csharp
public interface IReadOnlyReactiveValue<T> : IObservable<T>
{
    T Current { get; }
}
```

**Properties:**
- `Current`: Get the current value.

### IReactiveValue<T>

Mutable reactive value interface.

```csharp
public interface IReactiveValue<T> : IReadOnlyReactiveValue<T>
{
    void Set(T value);
    void Update(Func<T, T> updater);
}
```

**Methods:**
- `Set(T value)`: Set a new value.
- `Update(Func<T, T> updater)`: Update value using a function.

## Core Classes

### ReactiveFlow

Main entry point for creating reactive computations.

```csharp
public static class ReactiveFlow
{
    public static ReactiveScheduler DefaultScheduler { get; }
    
    public static ReactiveState<T> CreateState<T>(T initialValue = default(T));
    public static ReactiveEffect CreateEffect(string name, Action<ComputationBuilder> logic, params IObservable[] dependencies);
    public static ComputedValue<T> CreateComputed<T>(string name, Func<ComputationBuilder, T> computation, params IObservable[] dependencies);
    public static ConditionalComputation CreateConditional(string name, IReadOnlyReactiveValue<bool> condition, Action<ComputationBuilder> logic, params IObservable[] dependencies);
    public static DynamicComputationManager CreateDynamicManager();
    public static void ExecuteBatch(Action batchedOperations);
}
```

**Methods:**
- `CreateState<T>(T initialValue)`: Create a new reactive state.
- `CreateEffect(string name, Action<ComputationBuilder> logic, params IObservable[] dependencies)`: Create a side effect.
- `CreateComputed<T>(string name, Func<ComputationBuilder, T> computation, params IObservable[] dependencies)`: Create a computed value.
- `CreateConditional(...)`: Create a conditional computation.
- `CreateDynamicManager()`: Create a dynamic computation manager.
- `ExecuteBatch(Action batchedOperations)`: Execute operations in a batch.

### ReactiveState<T>

Mutable reactive state container.

```csharp
public class ReactiveState<T> : IReactiveValue<T>, IDisposable
{
    public ReactiveState(T initialValue = default(T), IEqualityComparer<T> equalityComparer = null);
    
    public T Current { get; }
    
    public void Set(T value);
    public void Update(Func<T, T> updater);
    public SubscriptionHandle Subscribe(Action callback);
    public SubscriptionHandle Subscribe(Action<T> callback);
    public void Unsubscribe(Action callback);
    public void Unsubscribe(Action<T> callback);
    public void Dispose();
}
```

**Constructor:**
- `ReactiveState(T initialValue, IEqualityComparer<T> equalityComparer)`: Create with initial value and optional equality comparer.

**Properties:**
- `Current`: Get current value.

**Methods:**
- `Set(T value)`: Set new value (only notifies if different).
- `Update(Func<T, T> updater)`: Update using a function.

### ComputedValue<T>

Cached computation that recalculates when dependencies change.

```csharp
public class ComputedValue<T> : ReactiveComputation, IReadOnlyReactiveValue<T>
{
    public ComputedValue(string name, ReactiveScheduler scheduler, Func<ComputationBuilder, T> computeFunction, params IObservable[] staticDependencies);
    
    public T Current { get; }
    
    public SubscriptionHandle Subscribe(Action callback);
    public SubscriptionHandle Subscribe(Action<T> callback);
    public void Unsubscribe(Action callback);
    public void Unsubscribe(Action<T> callback);
}
```

**Constructor:**
- `ComputedValue(string name, ReactiveScheduler scheduler, Func<ComputationBuilder, T> computeFunction, params IObservable[] staticDependencies)`: Create computed value.

**Properties:**
- `Current`: Get computed value (triggers recalculation if needed).

### ReactiveEffect

Side-effect computation that runs when dependencies change.

```csharp
public class ReactiveEffect : ReactiveComputation
{
    public ReactiveEffect(string name, ReactiveScheduler scheduler, Action<ComputationBuilder> logic, params IObservable[] staticDependencies);
}
```

**Constructor:**
- `ReactiveEffect(string name, ReactiveScheduler scheduler, Action<ComputationBuilder> logic, params IObservable[] staticDependencies)`: Create effect.

### ComputationBuilder

Provides API for building reactive computations with automatic dependency tracking.

```csharp
public class ComputationBuilder : IDisposable
{
    public ReactiveScheduler Scheduler { get; }
    
    // Dependency tracking
    public T Track<T>(IReadOnlyReactiveValue<T> reactiveValue);
    
    // Resource management
    public SubscriptionHandle Use(SubscriptionHandle handle);
    public T Use<T>(T disposable) where T : IDisposable;
    
    // Nested computations
    public IReadOnlyReactiveValue<T> CreateComputed<T>(Func<ComputationBuilder, T> computation, params IObservable[] dependencies);
    public void CreateEffect(Action<ComputationBuilder> logic, params IObservable[] dependencies);
    
    // Context management
    public T CreateContext<T>(T value);
    public T GetContext<T>();
    
    public void Dispose();
}
```

**Methods:**
- `Track<T>(IReadOnlyReactiveValue<T> reactiveValue)`: Track dependency and get current value.
- `Use<T>(T disposable)`: Manage disposable resource.
- `CreateComputed<T>(...)`: Create nested computed value.
- `CreateEffect(...)`: Create nested effect.
- `CreateContext<T>(T value)`: Set context value.
- `GetContext<T>()`: Get context value.

### EventSource<T>

Event source implementation for observables.

```csharp
public class EventSource<T> : IObservable<T>
{
    public EventSource();
    
    public SubscriptionHandle Subscribe(Action callback);
    public SubscriptionHandle Subscribe(Action<T> callback);
    public void Unsubscribe(Action callback);
    public void Unsubscribe(Action<T> callback);
    public void Emit(T value);
}
```

**Methods:**
- `Emit(T value)`: Emit a value to all subscribers.

### ReactiveScheduler

Orchestrates computation execution with dependency-aware scheduling.

```csharp
public class ReactiveScheduler
{
    public DeferredExecutionQueue DeferredQueue { get; }
    
    public void Schedule(ReactiveComputation computation);
    public void ExecuteBatch(Action batchedOperations);
}
```

**Methods:**
- `Schedule(ReactiveComputation computation)`: Schedule computation for execution.
- `ExecuteBatch(Action batchedOperations)`: Execute operations in a batch.

### DynamicComputationManager

Manages computations that can be created and destroyed dynamically.

```csharp
public class DynamicComputationManager : IDisposable
{
    public DynamicComputationManager(ReactiveScheduler scheduler);
    
    public void ManageEffect<TKey>(TKey key, Action<ComputationBuilder> logic, params IObservable[] dependencies);
    public void ManageComputed<TKey, T>(TKey key, Func<ComputationBuilder, T> computation, params IObservable[] dependencies);
    public void ManageDisposable<TKey>(TKey key, IDisposable disposable);
    public void Release<TKey>(TKey key);
    public void ReleaseAll();
    public void Dispose();
}
```

**Methods:**
- `ManageEffect<TKey>(TKey key, ...)`: Create or replace an effect with the given key.
- `ManageComputed<TKey, T>(TKey key, ...)`: Create or replace a computed value with the given key.
- `ManageDisposable<TKey>(TKey key, IDisposable disposable)`: Manage any disposable resource.
- `Release<TKey>(TKey key)`: Release resource associated with key.
- `ReleaseAll()`: Release all managed resources.

## Utility Types

### SubscriptionHandle

Handle for managing subscriptions.

```csharp
public class SubscriptionHandle
{
    public SubscriptionHandle(Action unsubscribeAction);
    public void Unsubscribe();
}
```

**Methods:**
- `Unsubscribe()`: Remove the subscription.

### ConditionalComputation

Computation that only runs when a condition is true.

```csharp
public class ConditionalComputation : ReactiveComputation
{
    public ConditionalComputation(string name, ReactiveScheduler scheduler, IReadOnlyReactiveValue<bool> condition, Action<ComputationBuilder> logic, params IObservable[] dependencies);
}
```

## Thread Safety

All core classes are thread-safe for concurrent access. However, computations always execute on the thread where the scheduler runs (main thread in Unity).

## Memory Management

- Always dispose of ReactiveEffect and ComputedValue instances when no longer needed
- ReactiveState implements IDisposable but disposal is optional (for cleanup)
- Use `ComputationBuilder.Use()` to automatically manage nested resources
- DynamicComputationManager automatically disposes managed resources

## Performance Notes

- Computations are batched and executed efficiently
- Equality comparers are used to prevent unnecessary notifications
- Dependency tracking has minimal overhead
- Memory usage scales linearly with the number of reactive values
