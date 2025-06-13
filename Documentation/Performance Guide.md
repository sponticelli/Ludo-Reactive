# Ludo.Reactive - Performance Guide

## Table of Contents
1. [Performance Overview](#performance-overview)
2. [Memory Management](#memory-management)
3. [Operator Optimization](#operator-optimization)
4. [Unity-Specific Optimizations](#unity-specific-optimizations)
5. [Platform Considerations](#platform-considerations)
6. [Profiling and Monitoring](#profiling-and-monitoring)
7. [Best Practices](#best-practices)
8. [Common Performance Pitfalls](#common-performance-pitfalls)

## Performance Overview

Ludo.Reactive is designed with performance as a core principle, featuring:

- **Zero-allocation operations** for most common scenarios
- **O(1) subscription/unsubscription** performance
- **Object pooling** for frequently allocated types
- **Lazy evaluation** throughout the framework
- **Efficient batching** for high-frequency events
- **Platform-specific optimizations** for WebGL and mobile

## Memory Management

### Automatic Disposal with AddTo()

Always use `.AddTo(this)` to prevent memory leaks:

```csharp
// ✅ Good - Automatic disposal
observable.Subscribe(value => DoSomething(value))
          .AddTo(this);

// ❌ Bad - Memory leak
observable.Subscribe(value => DoSomething(value));
```

### CompositeDisposable for Manual Management

```csharp
public class MySystem : MonoBehaviour
{
    private CompositeDisposable _disposables = new CompositeDisposable();
    
    void Start()
    {
        // Group related subscriptions
        _disposables.Add(
            observable1.Subscribe(HandleValue1)
        );
        _disposables.Add(
            observable2.Subscribe(HandleValue2)
        );
    }
    
    void OnDestroy()
    {
        _disposables.Dispose();
    }
}
```

### Avoiding Closure Allocations

```csharp
// ❌ Bad - Creates closure allocation
observable.Subscribe(value => ProcessValue(value, someLocalVariable));

// ✅ Good - Use instance methods when possible
observable.Subscribe(ProcessValue);

// ✅ Good - Or use Observer.Create for complex scenarios
observable.Subscribe(Observer.Create<int>(value => 
{
    // Complex logic here
}));
```

### ReactiveProperty Memory Optimization

```csharp
// ✅ Good - Use custom equality comparer for value types
var position = new ReactiveProperty<Vector3>(Vector3.zero, 
    new Vector3EqualityComparer(0.01f)); // Custom threshold

// ✅ Good - Use read-only properties to prevent external subscriptions
public IReadOnlyReactiveProperty<int> Health => _health.AsReadOnly();
```

## Operator Optimization

### Use DistinctUntilChanged() to Reduce Notifications

```csharp
// ✅ Good - Prevents unnecessary UI updates
playerPosition
    .Select(pos => Mathf.RoundToInt(pos.x))
    .DistinctUntilChanged()
    .Subscribe(x => UpdateUIPosition(x))
    .AddTo(this);

// ❌ Bad - Updates UI every frame even if position doesn't change significantly
playerPosition
    .Subscribe(pos => UpdateUIPosition(Mathf.RoundToInt(pos.x)))
    .AddTo(this);
```

### Throttle High-Frequency Events

```csharp
// ✅ Good - Throttle mouse movement
this.UpdateAsObservable()
    .Select(_ => Input.mousePosition)
    .Throttle(TimeSpan.FromMilliseconds(16)) // ~60 FPS
    .Subscribe(mousePos => UpdateCursor(mousePos))
    .AddTo(this);

// ❌ Bad - Updates every frame
this.UpdateAsObservable()
    .Select(_ => Input.mousePosition)
    .Subscribe(mousePos => UpdateCursor(mousePos))
    .AddTo(this);
```

### Filter Early in the Chain

```csharp
// ✅ Good - Filter before expensive operations
inputStream
    .Where(input => input.magnitude > 0.1f) // Filter first
    .Select(input => CalculateExpensiveMovement(input)) // Then transform
    .Subscribe(movement => ApplyMovement(movement))
    .AddTo(this);

// ❌ Bad - Expensive operation on every input
inputStream
    .Select(input => CalculateExpensiveMovement(input)) // Expensive operation first
    .Where(movement => movement.magnitude > 0.1f) // Filter after
    .Subscribe(movement => ApplyMovement(movement))
    .AddTo(this);
```

### Efficient Buffering

```csharp
// ✅ Good - Time-based buffering for batching
damageEvents
    .Buffer(TimeSpan.FromMilliseconds(100))
    .Where(damages => damages.Count > 0)
    .Subscribe(damages => ProcessBatchedDamage(damages))
    .AddTo(this);

// ✅ Good - Count-based buffering for fixed batches
inputEvents
    .Buffer(10) // Process in batches of 10
    .Subscribe(batch => ProcessInputBatch(batch))
    .AddTo(this);
```

## Unity-Specific Optimizations

### Scheduler Selection

```csharp
// ✅ Good - Use appropriate scheduler for the task
Observable.Interval(TimeSpan.FromSeconds(1))
    .ObserveOn(UnitySchedulers.MainThread) // UI updates
    .Subscribe(tick => UpdateUI(tick))
    .AddTo(this);

Observable.Interval(TimeSpan.FromSeconds(0.02f))
    .ObserveOn(UnitySchedulers.FixedUpdate) // Physics
    .Subscribe(tick => UpdatePhysics(tick))
    .AddTo(this);
```

### Lifecycle Observable Optimization

```csharp
// ✅ Good - Use global observables for shared events
Observable.EveryUpdate()
    .Where(_ => Input.GetKeyDown(KeyCode.Space))
    .Subscribe(_ => HandleSpaceKey())
    .AddTo(this);

// ❌ Bad - Creates separate Update observable per component
this.UpdateAsObservable()
    .Where(_ => Input.GetKeyDown(KeyCode.Space))
    .Subscribe(_ => HandleSpaceKey())
    .AddTo(this);
```

### UI Event Optimization

```csharp
// ✅ Good - Debounce input field changes
inputField.OnValueChangedAsObservable()
    .Throttle(TimeSpan.FromMilliseconds(300))
    .Subscribe(text => PerformSearch(text))
    .AddTo(this);

// ✅ Good - Batch button clicks
button.OnClickAsObservable()
    .Buffer(TimeSpan.FromMilliseconds(100))
    .Where(clicks => clicks.Count > 0)
    .Subscribe(clicks => HandleMultipleClicks(clicks.Count))
    .AddTo(this);
```

### Coroutine vs Observable Performance

```csharp
// ✅ Good - Use observables for event-driven logic
health
    .Where(h => h <= 0)
    .Take(1)
    .Subscribe(_ => TriggerDeath())
    .AddTo(this);

// ✅ Good - Use coroutines for sequential operations
Observable.FromCoroutine(() => LoadSequentialData())
    .Subscribe(_ => OnDataLoaded())
    .AddTo(this);

private IEnumerator LoadSequentialData()
{
    yield return LoadPlayerData();
    yield return LoadWorldData();
    yield return LoadUIData();
}
```

## Platform Considerations

### WebGL Optimizations

```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
// Use shorter intervals on WebGL
var updateInterval = TimeSpan.FromMilliseconds(33); // 30 FPS
#else
var updateInterval = TimeSpan.FromMilliseconds(16); // 60 FPS
#endif

Observable.Interval(updateInterval)
    .Subscribe(_ => UpdateGame())
    .AddTo(this);
```

### Mobile Optimizations

```csharp
// ✅ Good - Reduce update frequency on mobile
#if UNITY_ANDROID || UNITY_IOS
var throttleTime = TimeSpan.FromMilliseconds(50); // 20 FPS
#else
var throttleTime = TimeSpan.FromMilliseconds(16); // 60 FPS
#endif

this.UpdateAsObservable()
    .Throttle(throttleTime)
    .Subscribe(_ => UpdateMobileUI())
    .AddTo(this);
```

### Memory-Constrained Platforms

```csharp
// ✅ Good - Use object pooling on memory-constrained platforms
public class EffectPool : MonoBehaviour
{
    private Queue<GameObject> _pool = new Queue<GameObject>();
    
    public IObservable<GameObject> RentEffect()
    {
        return Observable.Create<GameObject>(observer =>
        {
            var effect = _pool.Count > 0 ? _pool.Dequeue() : CreateNewEffect();
            observer.OnNext(effect);
            observer.OnCompleted();
            
            // Auto-return after delay
            Observable.Timer(TimeSpan.FromSeconds(2))
                .Subscribe(_ => ReturnEffect(effect))
                .AddTo(this);
            
            return Disposable.Empty;
        });
    }
}
```

## Profiling and Monitoring

### Built-in Performance Monitoring

```csharp
public class ReactiveProfiler : MonoBehaviour
{
    private Subject<float> _frameTimeStream = new Subject<float>();
    
    void Start()
    {
        // Monitor frame times
        _frameTimeStream
            .Buffer(TimeSpan.FromSeconds(1))
            .Subscribe(frameTimes =>
            {
                var avgFrameTime = frameTimes.Average();
                var maxFrameTime = frameTimes.Max();
                
                if (maxFrameTime > 33) // Slower than 30 FPS
                {
                    Debug.LogWarning($"Performance spike: {maxFrameTime:F2}ms");
                }
            })
            .AddTo(this);
    }
    
    void Update()
    {
        _frameTimeStream.OnNext(Time.deltaTime * 1000f);
    }
}
```

### Subscription Monitoring

```csharp
public class SubscriptionMonitor : MonoBehaviour
{
    private static int _activeSubscriptions = 0;
    
    public static IDisposable TrackSubscription(IDisposable subscription)
    {
        _activeSubscriptions++;
        return Disposable.Create(() =>
        {
            subscription.Dispose();
            _activeSubscriptions--;
        });
    }
    
    void Update()
    {
        if (Time.frameCount % 60 == 0) // Log every second
        {
            Debug.Log($"Active subscriptions: {_activeSubscriptions}");
        }
    }
}
```

### Memory Usage Tracking

```csharp
public class MemoryMonitor : MonoBehaviour
{
    void Start()
    {
        Observable.Interval(TimeSpan.FromSeconds(5))
            .Subscribe(_ =>
            {
                var memoryUsage = System.GC.GetTotalMemory(false);
                Debug.Log($"Memory usage: {memoryUsage / 1024 / 1024:F2} MB");
                
                if (memoryUsage > 100 * 1024 * 1024) // 100 MB threshold
                {
                    System.GC.Collect();
                }
            })
            .AddTo(this);
    }
}
```

## Best Practices

### 1. Operator Ordering

```csharp
// ✅ Good - Optimal operator order
observable
    .Where(x => x.IsValid) // Filter first (cheapest)
    .DistinctUntilChanged() // Remove duplicates
    .Select(x => x.Transform()) // Transform (more expensive)
    .Throttle(TimeSpan.FromMilliseconds(16)) // Rate limit
    .Subscribe(ProcessValue)
    .AddTo(this);
```

### 2. Subscription Lifecycle

```csharp
// ✅ Good - Clear subscription lifecycle
public class GameManager : MonoBehaviour
{
    private CompositeDisposable _gameSubscriptions = new CompositeDisposable();
    
    void StartGame()
    {
        // Clear any existing subscriptions
        _gameSubscriptions.Clear();
        
        // Add new game-specific subscriptions
        _gameSubscriptions.Add(
            playerInput.Subscribe(HandleInput)
        );
    }
    
    void EndGame()
    {
        _gameSubscriptions.Clear();
    }
}
```

### 3. Efficient State Management

```csharp
// ✅ Good - Centralized state with minimal notifications
public class GameState : MonoBehaviour
{
    private ReactiveProperty<int> _score = new ReactiveProperty<int>(0);
    private ReactiveProperty<bool> _isPaused = new ReactiveProperty<bool>(false);
    
    // Expose read-only properties
    public IReadOnlyReactiveProperty<int> Score => _score.AsReadOnly();
    public IReadOnlyReactiveProperty<bool> IsPaused => _isPaused.AsReadOnly();
    
    // Batch state updates
    public void UpdateGameState(int scoreChange, bool pauseState)
    {
        _score.Value += scoreChange;
        _isPaused.Value = pauseState;
    }
}
```

## Common Performance Pitfalls

### 1. Excessive Subscriptions

```csharp
// ❌ Bad - Creates subscription for each enemy
foreach (var enemy in enemies)
{
    player.Position
        .Subscribe(pos => enemy.UpdateTarget(pos))
        .AddTo(enemy);
}

// ✅ Good - Single subscription with efficient distribution
player.Position
    .Subscribe(pos => 
    {
        foreach (var enemy in enemies)
            enemy.UpdateTarget(pos);
    })
    .AddTo(this);
```

### 2. Unnecessary Allocations

```csharp
// ❌ Bad - Allocates new Vector3 every frame
this.UpdateAsObservable()
    .Select(_ => new Vector3(transform.position.x, 0, transform.position.z))
    .Subscribe(pos => UpdateGroundPosition(pos))
    .AddTo(this);

// ✅ Good - Reuse or avoid allocations
private Vector3 _groundPosition;

this.UpdateAsObservable()
    .Subscribe(_ => 
    {
        _groundPosition.Set(transform.position.x, 0, transform.position.z);
        UpdateGroundPosition(_groundPosition);
    })
    .AddTo(this);
```

### 3. Inefficient Filtering

```csharp
// ❌ Bad - Complex filtering on every emission
observable
    .Where(item => ExpensiveValidation(item) && item.IsActive && item.Health > 0)
    .Subscribe(ProcessItem)
    .AddTo(this);

// ✅ Good - Filter cheap conditions first
observable
    .Where(item => item.IsActive) // Cheap check first
    .Where(item => item.Health > 0) // Another cheap check
    .Where(item => ExpensiveValidation(item)) // Expensive check last
    .Subscribe(ProcessItem)
    .AddTo(this);
```

### 4. Uncontrolled Event Frequency

```csharp
// ❌ Bad - No rate limiting on high-frequency events
mouseMovement
    .Subscribe(pos => UpdateCursor(pos))
    .AddTo(this);

// ✅ Good - Rate limit high-frequency events
mouseMovement
    .Throttle(TimeSpan.FromMilliseconds(16)) // 60 FPS max
    .Subscribe(pos => UpdateCursor(pos))
    .AddTo(this);
```

## Performance Checklist

- [ ] Use `.AddTo(this)` for all subscriptions
- [ ] Apply `DistinctUntilChanged()` where appropriate
- [ ] Use `Throttle()` for high-frequency events
- [ ] Filter early in operator chains
- [ ] Choose appropriate schedulers
- [ ] Monitor subscription count
- [ ] Profile memory usage regularly
- [ ] Use object pooling for frequently allocated objects
- [ ] Batch operations when possible
- [ ] Avoid unnecessary allocations in hot paths

---

Following these performance guidelines will ensure your Ludo.Reactive applications run efficiently across all Unity-supported platforms.
