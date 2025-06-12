# Ludo.Reactive - Architecture Design Document

## Table of Contents
1. [Overview](#overview)
2. [Core Architecture](#core-architecture)
3. [API Design](#api-design)
4. [Unity Integration](#unity-integration)
5. [Platform Considerations](#platform-considerations)
6. [Implementation Details](#implementation-details)
7. [Use Cases](#use-cases)
8. [Summary](#summary)

## 1. Overview 

### Purpose
Ludo.Reactive is a reactive programming framework designed specifically for Unity 6, providing developers with a powerful, performant, and Unity-native way to handle asynchronous data streams and event-driven programming patterns.

### Key Design Goals
- **Unity-First Design**: Seamless integration with Unity's component system, lifecycle, and editor
- **Performance**: Minimal garbage collection and optimized for mobile/WebGL platforms
- **Developer Experience**: Intuitive fluent API with comprehensive operator library
- **SOLID Compliance**: Maintainable, extensible architecture following clean code principles

## 2. Core Architecture

### 2.1 Foundation Classes

#### Observable<T>
The core abstraction representing a stream of values over time.

**Design Choice**: Using a generic interface allows type safety while maintaining flexibility.
            
```csharp
public interface IObservable<T>
{
    IDisposable Subscribe(IObserver<T> observer);
}

public interface IObserver<T>
{
    void OnNext(T value);
    void OnError(Exception error);
    void OnCompleted();
}
```

#### ReactiveProperty<T>
A mutable container that notifies observers when its value changes.

**Design Choice**: Combines the Observable pattern with property semantics familiar to Unity developers.

```csharp
public class ReactiveProperty<T> : IObservable<T>, IDisposable
{
    private T _value;
    private readonly Subject<T> _subject = new Subject<T>();
    
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                _subject.OnNext(value);
            }
        }
    }
}
```

### 2.2 Subject Types

#### Subject<T>
Both an observable and observer, acting as a bridge/proxy.

**Design Choice**: Essential for bridging imperative and reactive code, particularly useful for Unity Events integration.

#### BehaviorSubject<T>
Stores the latest value and emits it to new subscribers.

**Design Choice**: Perfect for state management in Unity components where late subscribers need the current state.

#### ReplaySubject<T>
Buffers a specified number of values and replays them to new subscribers.

**Design Choice**: Useful for event systems where components might need recent history (e.g., last N damage events).

### 2.3 Scheduler System

**Design Choice**: Abstract scheduling to support Unity's various execution contexts.

```csharp
public interface IScheduler
{
    IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action);
    IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action);
}

public static class UnitySchedulers
{
    public static IScheduler MainThread { get; }      // Unity main thread
    public static IScheduler FixedUpdate { get; }     // FixedUpdate timing
    public static IScheduler LateUpdate { get; }      // LateUpdate timing
    public static IScheduler EndOfFrame { get; }      // End of frame timing
}
```

## 3. API Design

### 3.1 Fluent API Structure

**Design Choice**: Method chaining provides readable, composable operations that mirror LINQ patterns familiar to C# developers.

```csharp
// Example usage
player.Health
    .Where(health => health > 0)
    .Select(health => health / maxHealth)
    .DistinctUntilChanged()
    .Subscribe(ratio => healthBar.fillAmount = ratio)
    .AddTo(this); // Unity-specific: auto-dispose with GameObject
```

### 3.2 Core Operators

#### Creation Operators
```csharp
public static class Observable
{
    // Create from Unity-specific sources
    public static IObservable<Unit> FromCoroutine(Func<IEnumerator> coroutine);
    public static IObservable<T> FromUnityEvent<T>(UnityEvent<T> unityEvent);
    public static IObservable<AsyncOperation> FromAsyncOperation(AsyncOperation operation);
    
    // Standard creation
    public static IObservable<T> Return<T>(T value);
    public static IObservable<T> Empty<T>();
    public static IObservable<T> Never<T>();
    public static IObservable<long> Interval(TimeSpan period);
}
```

#### Transformation Operators
```csharp
public static class ObservableExtensions
{
    // Map/Select - transform values
    public static IObservable<TResult> Select<T, TResult>(
        this IObservable<T> source, 
        Func<T, TResult> selector);
    
    // FlatMap/SelectMany - flatten nested observables
    public static IObservable<TResult> SelectMany<T, TResult>(
        this IObservable<T> source,
        Func<T, IObservable<TResult>> selector);
    
    // Buffer - collect values over time/count
    public static IObservable<IList<T>> Buffer<T>(
        this IObservable<T> source,
        TimeSpan timeSpan);
}
```

#### Filtering Operators
```csharp
public static class ObservableExtensions
{
    public static IObservable<T> Where<T>(
        this IObservable<T> source,
        Func<T, bool> predicate);
    
    public static IObservable<T> DistinctUntilChanged<T>(
        this IObservable<T> source);
    
    public static IObservable<T> Throttle<T>(
        this IObservable<T> source,
        TimeSpan dueTime);
}
```

#### Aggregation Operators
```csharp
public static class ObservableExtensions
{
    public static IObservable<TAccumulate> Scan<T, TAccumulate>(
        this IObservable<T> source,
        TAccumulate seed,
        Func<TAccumulate, T, TAccumulate> accumulator);
    
    public static IObservable<TResult> Aggregate<T, TAccumulate, TResult>(
        this IObservable<T> source,
        TAccumulate seed,
        Func<TAccumulate, T, TAccumulate> accumulator,
        Func<TAccumulate, TResult> resultSelector);
}
```

### 3.3 SOLID Principles Implementation

#### Single Responsibility Principle
Each operator is a separate extension method with a single, well-defined purpose.

#### Open/Closed Principle
The framework is extensible through extension methods without modifying core classes.

```csharp
// Custom operator example
public static IObservable<T> WhereNotNull<T>(this IObservable<T> source) 
    where T : class
{
    return source.Where(x => x != null);
}
```

#### Liskov Substitution Principle
All observables implement IObservable<T> and can be used interchangeably.

#### Interface Segregation Principle
Separate interfaces for different concerns:
- IObservable<T> for sources
- IObserver<T> for sinks
- IScheduler for timing
- IDisposable for cleanup

#### Dependency Inversion Principle
Depend on abstractions (interfaces) rather than concrete implementations.

## 4. Unity Integration

### 4.1 MonoBehaviour Integration

**Design Choice**: Extension methods that respect Unity's component lifecycle.

```csharp
public static class UnityObservableExtensions
{
    // Auto-dispose when GameObject is destroyed
    public static T AddTo<T>(this T disposable, Component component) 
        where T : IDisposable
    {
        component.GetOrAddComponent<DisposableTracker>().Add(disposable);
        return disposable;
    }
    
    // Observe Unity lifecycle events
    public static IObservable<Unit> OnDestroyAsObservable(this Component component);
    public static IObservable<Unit> OnEnableAsObservable(this Component component);
    public static IObservable<Unit> UpdateAsObservable(this Component component);
}
```

### 4.2 Coroutine Support

**Design Choice**: Bidirectional conversion between coroutines and observables for gradual migration.

```csharp
public static class CoroutineObservableExtensions
{
    // Coroutine to Observable
    public static IObservable<T> ToObservable<T>(this IEnumerator coroutine);
    
    // Observable to Coroutine
    public static IEnumerator ToCoroutine<T>(
        this IObservable<T> source,
        Action<T> onNext = null,
        Action<Exception> onError = null);
}
```

### 4.3 Async/Await Support

**Design Choice**: Seamless integration with Unity's async operations and C# async/await.

```csharp
public static class AsyncObservableExtensions
{
    // Observable to Task
    public static Task<T> ToTask<T>(this IObservable<T> source);
    public static Task<T[]> ToArrayAsync<T>(this IObservable<T> source);
    
    // Task to Observable
    public static IObservable<T> ToObservable<T>(this Task<T> task);
    
    // Unity async operations
    public static IObservable<T> ToObservable<T>(this AsyncOperation operation);
}
```

### 4.4 Unity Events Bridge

**Design Choice**: Wrapper pattern to preserve existing UnityEvent functionality while adding reactive capabilities.

```csharp
public static class UnityEventExtensions
{
    public static IObservable<Unit> AsObservable(this UnityEvent unityEvent)
    {
        return Observable.Create<Unit>(observer =>
        {
            UnityAction handler = () => observer.OnNext(Unit.Default);
            unityEvent.AddListener(handler);
            return Disposable.Create(() => unityEvent.RemoveListener(handler));
        });
    }
    
    public static IObservable<T> AsObservable<T>(this UnityEvent<T> unityEvent);
}
```

### 4.5 Editor Integration

#### Custom Property Drawers
```csharp
[CustomPropertyDrawer(typeof(ReactiveProperty<>))]
public class ReactivePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Display current value
        // Show subscription count
        // Provide manual trigger button in debug mode
    }
}
```

#### Debug Window
```csharp
public class ReactiveDebugWindow : EditorWindow
{
    // Visualize active subscriptions
    // Show marble diagrams for debugging
    // Performance metrics (allocation tracking)
}
```

## 5. Platform Considerations

### 5.1 WebGL Compatibility

**Design Choices:**
- No threading: All operations on main thread by default
- Minimal reflection: AOT-friendly implementation
- Small build size: Modular design allows dead code stripping

```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
public static class WebGLScheduler : IScheduler
{
    // Implementation using setTimeout/requestAnimationFrame
}
#endif
```

### 5.2 Mobile Performance

**Design Choices:**
- Object pooling for frequently allocated types
- Struct-based operators where possible
- Lazy evaluation throughout

```csharp
// Object pool for reducing allocations
internal static class ObserverPool<T>
{
    private static readonly Stack<PooledObserver<T>> _pool = new();
    
    public static IObserver<T> Rent(Action<T> onNext)
    {
        if (_pool.Count > 0)
        {
            var observer = _pool.Pop();
            observer.Initialize(onNext);
            return observer;
        }
        return new PooledObserver<T>(onNext);
    }
}
```

### 5.3 Memory Management

**Design Choice**: Automatic disposal integration with Unity lifecycle.

```csharp
public class CompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    
    public void Add(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }
    
    public void Dispose()
    {
        foreach (var d in _disposables)
            d?.Dispose();
        _disposables.Clear();
    }
}
```

## 6. Implementation Details

### 6.1 Error Handling

**Design Choice**: Graceful error propagation with Unity-specific logging.

```csharp
public static class ObservableExtensions
{
    public static IObservable<T> Catch<T>(
        this IObservable<T> source,
        Func<Exception, IObservable<T>> handler)
    {
        // Catch and recover from errors
    }
    
    public static IObservable<T> LogError<T>(
        this IObservable<T> source,
        string context = null)
    {
        return source.Do(
            onNext: _ => { },
            onError: ex => Debug.LogError($"[Ludo.Reactive] {context}: {ex}"),
            onCompleted: () => { }
        );
    }
}
```

### 6.2 Testing Support

**Design Choice**: Time-travel debugging and deterministic testing.

```csharp
public class TestScheduler : IScheduler
{
    // Allows manual time advancement for testing
    public void AdvanceBy(TimeSpan time);
    public void AdvanceTo(DateTimeOffset time);
}
```

### 6.3 Performance Monitoring

```csharp
[Conditional("LUDO_REACTIVE_PROFILING")]
public static class ReactiveProfiler
{
    public static void BeginSample(string name);
    public static void EndSample();
    
    // Integration with Unity Profiler
    public static ProfilerMarker CreateMarker(string name);
}
```


## 7. Use Cases

### 7.1 Game State Management

#### Health System
**Problem**: Managing player health with various modifiers, UI updates, and death conditions.

**Traditional Approach**: Scattered update calls, event handlers, and null checks across multiple scripts.

**Ludo.Reactive Solution**:
```csharp
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private ReactiveProperty<float> _currentHealth = new(100f);
    [SerializeField] private ReactiveProperty<float> _maxHealth = new(100f);
    
    public IReadOnlyReactiveProperty<float> CurrentHealth => _currentHealth;
    public IReadOnlyReactiveProperty<float> MaxHealth => _maxHealth;
    
    // Health percentage for UI
    public IObservable<float> HealthPercentage => 
        _currentHealth.CombineLatest(_maxHealth, (current, max) => current / max);
    
    // Death detection
    public IObservable<Unit> OnDeath => 
        _currentHealth
            .Where(health => health <= 0)
            .Take(1)
            .AsUnitObservable();
    
    void Start()
    {
        // Auto-save when health changes
        _currentHealth
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(_ => SaveManager.SaveHealth(_currentHealth.Value))
            .AddTo(this);
            
        // Damage over time effects
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Where(_ => isPoisoned)
            .Subscribe(_ => TakeDamage(poisonDamage))
            .AddTo(this);
    }
}
```

**Benefits**:
- Centralized health state management
- Automatic UI updates
- Composable health effects
- Memory-safe with automatic disposal

#### Combo System
**Problem**: Tracking player input sequences with timing windows.

**Ludo.Reactive Solution**:
```csharp
public class ComboSystem : MonoBehaviour
{
    [SerializeField] private float comboWindowSeconds = 0.5f;
    
    private Subject<KeyCode> _inputStream = new Subject<KeyCode>();
    
    void Start()
    {
        // Detect specific combo: Up, Up, Down, Down, A
        var targetCombo = new[] { KeyCode.W, KeyCode.W, KeyCode.S, KeyCode.S, KeyCode.A };
        
        _inputStream
            .Buffer(targetCombo.Length, 1)
            .Where(buffer => buffer.SequenceEqual(targetCombo))
            .Subscribe(_ => ExecuteSpecialMove())
            .AddTo(this);
        
        // Reset combo on timeout
        _inputStream
            .Throttle(TimeSpan.FromSeconds(comboWindowSeconds))
            .Subscribe(_ => ResetComboMultiplier())
            .AddTo(this);
    }
    
    void Update()
    {
        if (Input.anyKeyDown)
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                    _inputStream.OnNext(key);
            }
        }
    }
}
```

### 7.2 UI Reactive Bindings

#### Inventory UI
**Problem**: Keeping UI in sync with constantly changing inventory data.

**Ludo.Reactive Solution**:
```csharp
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Text goldText;
    [SerializeField] private Button sortButton;
    
    private ReactiveCollection<Item> _items = new ReactiveCollection<Item>();
    private ReactiveProperty<int> _gold = new ReactiveProperty<int>(0);
    
    void Start()
    {
        // Auto-update gold display
        _gold
            .Subscribe(gold => goldText.text = $"Gold: {gold:N0}")
            .AddTo(this);
        
        // Reactive item list
        _items.ObserveAdd()
            .Subscribe(addEvent => CreateItemUI(addEvent.Value))
            .AddTo(this);
            
        _items.ObserveRemove()
            .Subscribe(removeEvent => RemoveItemUI(removeEvent.Value))
            .AddTo(this);
        
        // Sort button with cooldown
        sortButton.OnClickAsObservable()
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(_ => SortInventory())
            .AddTo(this);
        
        // Highlight new items
        _items.ObserveAdd()
            .Select(x => x.Value)
            .Delay(TimeSpan.FromSeconds(0.1f))
            .Subscribe(item => HighlightNewItem(item))
            .AddTo(this);
    }
}
```

#### Reactive Forms
**Problem**: Form validation with real-time feedback.

**Ludo.Reactive Solution**:
```csharp
public class LoginForm : MonoBehaviour
{
    [SerializeField] private InputField usernameInput;
    [SerializeField] private InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Text errorText;
    
    void Start()
    {
        var usernameValid = usernameInput.OnValueChangedAsObservable()
            .Select(text => text.Length >= 3);
            
        var passwordValid = passwordInput.OnValueChangedAsObservable()
            .Select(text => text.Length >= 8);
        
        // Enable login button only when both fields are valid
        usernameValid.CombineLatest(passwordValid, (u, p) => u && p)
            .Subscribe(valid => loginButton.interactable = valid)
            .AddTo(this);
        
        // Show validation errors with debounce
        usernameInput.OnValueChangedAsObservable()
            .Where(text => text.Length > 0 && text.Length < 3)
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(_ => ShowError("Username must be at least 3 characters"))
            .AddTo(this);
    }
}
```

### 7.3 Animation and Visual Effects

#### Damage Numbers
**Problem**: Spawning floating damage numbers with pooling and animation.

**Ludo.Reactive Solution**:
```csharp
public class DamageNumberSystem : MonoBehaviour
{
    private Subject<DamageEvent> _damageStream = new Subject<DamageEvent>();
    
    void Start()
    {
        // Batch damage for combined display
        _damageStream
            .Buffer(TimeSpan.FromMilliseconds(100))
            .Where(damages => damages.Count > 0)
            .Subscribe(damages =>
            {
                var total = damages.Sum(d => d.Amount);
                var position = damages.First().WorldPosition;
                SpawnDamageNumber(total, position, damages.Count > 1);
            })
            .AddTo(this);
        
        // Critical hit effects
        _damageStream
            .Where(damage => damage.IsCritical)
            .Subscribe(damage => PlayCriticalHitEffect(damage.WorldPosition))
            .AddTo(this);
    }
    
    public void ReportDamage(float amount, Vector3 position, bool isCritical)
    {
        _damageStream.OnNext(new DamageEvent 
        { 
            Amount = amount, 
            WorldPosition = position, 
            IsCritical = isCritical 
        });
    }
}
```

#### Smooth Camera Follow
**Problem**: Creating smooth, responsive camera movement with multiple targets.

**Ludo.Reactive Solution**:
```csharp
public class CameraController : MonoBehaviour
{
    [SerializeField] private float smoothTime = 0.3f;
    private ReactiveProperty<Transform> _target = new ReactiveProperty<Transform>();
    
    void Start()
    {
        // Smooth position following
        _target
            .Where(t => t != null)
            .Select(t => t.position)
            .DistinctUntilChanged()
            .Subscribe(targetPos =>
            {
                Observable.EveryUpdate()
                    .TakeUntil(_target.Where(x => x == null))
                    .Subscribe(_ =>
                    {
                        var smooth = Vector3.Lerp(transform.position, targetPos, Time.deltaTime / smoothTime);
                        transform.position = new Vector3(smooth.x, smooth.y, transform.position.z);
                    })
                    .AddTo(this);
            })
            .AddTo(this);
        
        // Shake effect on damage
        MessageBroker.Default.Receive<PlayerDamagedEvent>()
            .Subscribe(_ => StartCoroutine(CameraShake()))
            .AddTo(this);
    }
}
```

### 7.4 Networking and Multiplayer

#### Network Message Handling
**Problem**: Managing incoming network messages with different types and handlers.

**Ludo.Reactive Solution**:
```csharp
public class NetworkManager : MonoBehaviour
{
    private Subject<NetworkMessage> _messageStream = new Subject<NetworkMessage>();
    
    void Start()
    {
        // Player movement messages
        _messageStream
            .OfType<NetworkMessage, PlayerMoveMessage>()
            .Where(msg => msg.PlayerId != localPlayerId)
            .Subscribe(msg => UpdatePlayerPosition(msg.PlayerId, msg.Position))
            .AddTo(this);
        
        // Chat messages with spam protection
        _messageStream
            .OfType<NetworkMessage, ChatMessage>()
            .GroupBy(msg => msg.SenderId)
            .SelectMany(group => group.Throttle(TimeSpan.FromSeconds(1)))
            .Subscribe(msg => DisplayChatMessage(msg))
            .AddTo(this);
        
        // Connection status
        Observable.Interval(TimeSpan.FromSeconds(5))
            .SelectMany(_ => Observable.FromCoroutine<bool>(CheckConnection))
            .DistinctUntilChanged()
            .Subscribe(connected => UpdateConnectionUI(connected))
            .AddTo(this);
    }
}
```

#### Multiplayer Sync
**Problem**: Synchronizing game state across clients with conflict resolution.

**Ludo.Reactive Solution**:
```csharp
public class MultiplayerSync : MonoBehaviour
{
    private ReactiveProperty<GameState> _localState = new ReactiveProperty<GameState>();
    private Subject<GameState> _remoteState = new Subject<GameState>();
    
    void Start()
    {
        // Merge local and remote states
        var mergedState = _localState
            .CombineLatest(_remoteState, (local, remote) => 
                ResolveConflicts(local, remote))
            .DistinctUntilChanged();
        
        // Broadcast local changes
        _localState
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Where(_ => isHost)
            .Subscribe(state => BroadcastState(state))
            .AddTo(this);
        
        // Handle disconnections
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Where(_ => Time.time - lastReceivedTime > disconnectTimeout)
            .Take(1)
            .Subscribe(_ => HandleDisconnection())
            .AddTo(this);
    }
}
```

### 7.5 Resource Management

#### Asset Loading
**Problem**: Loading assets asynchronously with progress tracking and caching.

**Ludo.Reactive Solution**:
```csharp
public class AssetLoader : MonoBehaviour
{
    private Dictionary<string, IObservable<Object>> _assetCache = new();
    
    public IObservable<T> LoadAsset<T>(string path) where T : Object
    {
        if (!_assetCache.ContainsKey(path))
        {
            _assetCache[path] = Observable.FromCoroutine<T>(observer => 
                LoadAssetCoroutine(path, observer))
                .Replay(1)
                .RefCount();
        }
        
        return _assetCache[path].Cast<Object, T>();
    }
    
    public IObservable<float> LoadSceneWithProgress(string sceneName)
    {
        return Observable.Create<float>(observer =>
        {
            var operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;
            
            return Observable.EveryUpdate()
                .Select(_ => operation.progress)
                .TakeUntil(Observable.Return(Unit.Default)
                    .Delay(TimeSpan.FromSeconds(0.1f))
                    .Where(_ => operation.progress >= 0.9f))
                .DoOnCompleted(() => operation.allowSceneActivation = true)
                .Subscribe(observer);
        });
    }
}
```

#### Object Pooling
**Problem**: Managing object pools with automatic return and cleanup.

**Ludo.Reactive Solution**:
```csharp
public class ReactiveObjectPool<T> where T : Component
{
    private readonly Stack<T> _pool = new Stack<T>();
    private readonly Subject<T> _returnStream = new Subject<T>();
    
    public ReactiveObjectPool(T prefab, int initialSize)
    {
        // Auto-return objects after delay
        _returnStream
            .Delay(TimeSpan.FromSeconds(5))
            .Where(obj => obj.gameObject.activeSelf)
            .Subscribe(obj => Return(obj));
        
        // Pre-warm pool
        Observable.Range(0, initialSize)
            .Subscribe(_ => 
            {
                var obj = Object.Instantiate(prefab);
                obj.gameObject.SetActive(false);
                _pool.Push(obj);
            });
    }
    
    public IObservable<T> Rent()
    {
        return Observable.Create<T>(observer =>
        {
            var obj = _pool.Count > 0 ? _pool.Pop() : CreateNew();
            obj.gameObject.SetActive(true);
            observer.OnNext(obj);
            observer.OnCompleted();
            return Disposable.Empty;
        });
    }
}
```

### 7.6 Audio System

#### Dynamic Music System
**Problem**: Creating adaptive music that responds to game events.

**Ludo.Reactive Solution**:
```csharp
public class DynamicMusicSystem : MonoBehaviour
{
    private ReactiveProperty<MusicState> _currentState = new(MusicState.Exploration);
    private ReactiveProperty<float> _intensity = new(0f);
    
    void Start()
    {
        // Smooth intensity transitions
        _intensity
            .Select(target => 
                Observable.EveryUpdate()
                    .Scan(audioMixer.GetFloat("Intensity", out float current) ? current : 0f,
                        (acc, _) => Mathf.Lerp(acc, target, Time.deltaTime * 2f))
                    .TakeUntil(_intensity.Skip(1)))
            .Switch()
            .Subscribe(value => audioMixer.SetFloat("Intensity", value))
            .AddTo(this);
        
        // Music state transitions
        _currentState
            .DistinctUntilChanged()
            .Subscribe(state => CrossfadeTo(GetMusicTrack(state)))
            .AddTo(this);
        
        // Combat music triggers
        Observable.Merge(
            EnemyManager.OnCombatStart.Select(_ => MusicState.Combat),
            EnemyManager.OnCombatEnd.Delay(TimeSpan.FromSeconds(3)).Select(_ => MusicState.Exploration)
        )
        .Subscribe(state => _currentState.Value = state)
        .AddTo(this);
    }
}
```

### 7.7 AI and Behavior Trees

#### Reactive AI State Machine
**Problem**: Creating responsive AI that reacts to multiple stimuli.

**Ludo.Reactive Solution**:
```csharp
public class EnemyAI : MonoBehaviour
{
    private ReactiveProperty<AIState> _currentState = new(AIState.Patrol);
    private Subject<Transform> _targetSpotted = new Subject<Transform>();
    
    void Start()
    {
        // State transitions based on events
        var stateTransitions = Observable.Merge(
            // Spot player -> Chase
            _targetSpotted.Where(t => t != null).Select(_ => AIState.Chase),
            // Lose player -> Search
            _targetSpotted.Where(t => t == null).Delay(TimeSpan.FromSeconds(2)).Select(_ => AIState.Search),
            // Health low -> Flee
            health.Where(h => h < 20).Select(_ => AIState.Flee),
            // Search timeout -> Patrol
            _currentState.Where(s => s == AIState.Search)
                .Delay(TimeSpan.FromSeconds(10))
                .Select(_ => AIState.Patrol)
        );
        
        stateTransitions
            .Subscribe(newState => _currentState.Value = newState)
            .AddTo(this);
        
        // Execute behavior based on state
        _currentState
            .DistinctUntilChanged()
            .Subscribe(state => ExecuteStateBehavior(state))
            .AddTo(this);
    }
}
```

### 7.8 Save System

#### Auto-Save with Dirty Tracking
**Problem**: Efficiently saving only changed data at appropriate times.

**Ludo.Reactive Solution**:
```csharp
public class SaveManager : MonoBehaviour
{
    private Subject<ISaveable> _dirtyItems = new Subject<ISaveable>();
    private CompositeDisposable _saveDisposables = new CompositeDisposable();
    
    void Start()
    {
        // Batch saves every 30 seconds
        _dirtyItems
            .Buffer(TimeSpan.FromSeconds(30))
            .Where(items => items.Count > 0)
            .Subscribe(items => SaveBatch(items.Distinct()))
            .AddTo(this);
        
        // Save on specific events
        Observable.Merge(
            SceneManager.sceneLoaded.AsObservable().Select(_ => Unit.Default),
            OnApplicationPause.Where(paused => paused).Select(_ => Unit.Default),
            OnApplicationFocus.Where(focused => !focused).Select(_ => Unit.Default)
        )
        .Subscribe(_ => ForceSave())
        .AddTo(this);
        
        // Track player stats changes
        player.Stats.Health
            .Merge(player.Stats.Mana)
            .Merge(player.Stats.Experience)
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(_ => _dirtyItems.OnNext(player.Stats))
            .AddTo(this);
    }
}
```

### 7.9 Tutorial System

#### Reactive Tutorial Flow
**Problem**: Creating tutorials that respond to player actions dynamically.

**Ludo.Reactive Solution**:
```csharp
public class TutorialManager : MonoBehaviour
{
    private Subject<TutorialEvent> _eventStream = new Subject<TutorialEvent>();
    
    void Start()
    {
        // Movement tutorial
        var movementComplete = _eventStream
            .Where(e => e.Type == TutorialEventType.PlayerMoved)
            .Scan(0, (count, _) => count + 1)
            .Where(count => count >= 5)
            .Take(1);
        
        // Combat tutorial triggers after movement
        movementComplete
            .Delay(TimeSpan.FromSeconds(1))
            .Subscribe(_ => StartCombatTutorial())
            .AddTo(this);
        
        // Skip tutorial on experienced player behavior
        _eventStream
            .Where(e => e.Type == TutorialEventType.AdvancedAction)
            .Take(1)
            .Subscribe(_ => SkipToAdvancedTutorial())
            .AddTo(this);
        
        // Hint system
        _eventStream
            .Where(e => e.Type == TutorialEventType.PlayerStuck)
            .Throttle(TimeSpan.FromSeconds(10))
            .Subscribe(_ => ShowHint())
            .AddTo(this);
    }
}
```

### 7.10 Performance Monitoring

#### FPS and Performance Metrics
**Problem**: Monitoring performance and reacting to frame drops.

**Ludo.Reactive Solution**:
```csharp
public class PerformanceManager : MonoBehaviour
{
    private Subject<float> _frameTimeStream = new Subject<float>();
    
    void Start()
    {
        // Calculate FPS
        var fps = _frameTimeStream
            .Buffer(TimeSpan.FromSeconds(1))
            .Select(times => times.Count);
        
        // Detect performance issues
        fps.Where(f => f < 30)
            .Throttle(TimeSpan.FromSeconds(5))
            .Subscribe(_ => ReduceQualitySettings())
            .AddTo(this);
        
        // Log performance spikes
        _frameTimeStream
            .Where(frameTime => frameTime > 50) // 50ms = 20fps
            .Buffer(TimeSpan.FromSeconds(1))
            .Where(spikes => spikes.Count > 5)
            .Subscribe(spikes => LogPerformanceIssue(spikes))
            .AddTo(this);
        
        // Auto-adjust quality based on average FPS
        fps.Buffer(10, 1)
            .Select(samples => samples.Average())
            .DistinctUntilChanged(avg => Mathf.RoundToInt(avg / 10))
            .Subscribe(avgFps => AdjustQualitySettings(avgFps))
            .AddTo(this);
    }
    
    void Update()
    {
        _frameTimeStream.OnNext(Time.deltaTime * 1000f);
    }
}
```

## Summary of Use Cases

These use cases demonstrate how Ludo.Reactive solves common Unity development challenges:

1. **Simplifies Complex Event Chains**: Chain operations naturally without callback hell
2. **Reduces Boilerplate**: Automatic subscription management and disposal
3. **Improves Performance**: Built-in throttling, buffering, and optimization operators
4. **Enhances Maintainability**: Declarative code that clearly expresses intent
5. **Enables Composition**: Small, reusable reactive components that combine easily
6. **Supports Testing**: Time-based operations become testable with schedulers
7. **Prevents Memory Leaks**: Automatic cleanup with Unity lifecycle integration

By adopting Ludo.Reactive, developers can focus on game logic rather than event management plumbing, resulting in cleaner, more maintainable, and more performant Unity applications.

# Summary

Ludo.Reactive provides a comprehensive reactive programming solution tailored specifically for Unity 6 development. By prioritizing Unity integration, performance, and developer experience, it enables powerful asynchronous programming patterns while maintaining the familiar Unity workflow. The architecture's adherence to SOLID principles ensures long-term maintainability and extensibility, while platform-specific optimizations guarantee production-ready performance across all Unity-supported platforms.