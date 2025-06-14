# Ludo.Reactive

A high-performance reactive programming framework designed specifically for Unity 6, providing developers with powerful tools for handling asynchronous data streams, event-driven programming, state management, reactive collections, and automatic UI binding.

## ✨ Features

### Core Reactive Programming
- **🎯 Unity-First Design**: Seamless integration with Unity's component system, lifecycle, and editor
- **⚡ High Performance**: Minimal garbage collection, optimized for mobile/WebGL platforms
- **🔧 Developer-Friendly**: Intuitive fluent API with comprehensive operator library
- **🏗️ SOLID Architecture**: Maintainable, extensible design following clean code principles
- **🔄 Automatic Memory Management**: Built-in disposal tracking with Unity lifecycle integration
- **📱 Platform Optimized**: WebGL and mobile-ready with object pooling and efficient algorithms

### State Management (v1.2.0)
- **🏪 Redux-like Store**: Immutable state management with action dispatching
- **⏪ Undo/Redo System**: Complete command history with reversible operations
- **💾 State Persistence**: Save/load state with multiple storage backends
- **🎯 State Selectors**: Efficient state slicing with memoization
- **🔧 Middleware Support**: Extensible action processing pipeline

### Reactive Collections (v1.2.0)
- **📋 ObservableList**: List with granular change tracking and batch operations
- **📚 ObservableDictionary**: Dictionary with key/value change notifications
- **🎯 ObservableSet**: Set with add/remove notifications and set operations
- **🔄 Collection Synchronization**: Multi-collection sync with conflict resolution
- **📊 Efficient Diffing**: Myers' algorithm for optimal collection updates

## 🚀 Quick Start

### Installation

The package is already included in your Unity project. Add the namespace to your scripts:

```csharp
using Ludo.Reactive;
```

### Basic Usage

```csharp
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private ReactiveProperty<int> _health = new ReactiveProperty<int>(100);
    [SerializeField] private Slider healthSlider;
    
    public IReadOnlyReactiveProperty<int> Health => _health.AsReadOnly();
    
    void Start()
    {
        // Bind health to UI slider
        _health
            .Select(health => health / 100f)
            .Subscribe(percentage => healthSlider.value = percentage)
            .AddTo(this); // Automatically dispose when GameObject is destroyed
        
        // Death detection
        _health
            .Where(health => health <= 0)
            .Take(1)
            .Subscribe(_ => OnPlayerDeath())
            .AddTo(this);
    }
    
    public void TakeDamage(int damage)
    {
        _health.Value = Mathf.Max(0, _health.Value - damage);
    }
}
```

### Unity Integration

```csharp
void Start()
{
    // Observe button clicks
    button.OnClickAsObservable()
        .Throttle(TimeSpan.FromSeconds(0.5f)) // Prevent spam
        .Subscribe(_ => Fire())
        .AddTo(this);
    
    // Observe Unity lifecycle
    this.UpdateAsObservable()
        .Where(_ => Input.GetKeyDown(KeyCode.Space))
        .Subscribe(_ => Jump())
        .AddTo(this);
    
    // Combine multiple data sources
    health.CombineLatest(mana, (h, m) => h > 0 && m >= spellCost)
          .Subscribe(canCast => spellButton.interactable = canCast)
          .AddTo(this);
}
```

## 🏗️ Core Architecture

### ReactiveProperty<T>
Mutable container that notifies observers when its value changes:

```csharp
var health = new ReactiveProperty<int>(100);
health.Subscribe(value => Debug.Log($"Health: {value}"));
health.Value = 80; // Triggers notification
```

### Observable Sequences
Create and manipulate streams of data:

```csharp
Observable.Interval(TimeSpan.FromSeconds(1))
    .Take(10)
    .Where(x => x % 2 == 0)
    .Subscribe(x => Debug.Log($"Even number: {x}"));
```

### Subjects
Bridge between imperative and reactive code:

```csharp
var damageEvents = new Subject<int>();
damageEvents.Subscribe(damage => ApplyDamage(damage));
damageEvents.OnNext(25); // Trigger damage
```

## 🏪 State Management (v1.2.0)

### ReactiveStore
Redux-like state management with immutable updates:

```csharp
// Define your state
public class GameState
{
    public int Level { get; set; }
    public int Gold { get; set; }
    public string PlayerName { get; set; }
}

// Create store with reducer
var store = new ReactiveStore<GameState>(
    initialState: new GameState { Level = 1, Gold = 100, PlayerName = "Player" },
    reducer: new GameStateReducer()
);

// Subscribe to state changes
store.StateChanged.Subscribe(change =>
    Debug.Log($"State changed by {change.Action}"));

// Dispatch actions
store.Dispatch(new LevelUpAction());
```

### State Selectors
Efficiently select and observe parts of state:

```csharp
// Create memoized selectors
var levelSelector = store.Select(state => state.Level, "LevelSelector");
var goldSelector = store.Select(state => state.Gold, "GoldSelector");

// Subscribe to specific state changes
levelSelector.Subscribe(level => UpdateLevelUI(level));
goldSelector.Subscribe(gold => UpdateGoldUI(gold));
```

### Undo/Redo System
Complete command history with reversible operations:

```csharp
var commandHistory = new CommandHistory<GameState>();

// Execute commands
var newState = commandHistory.ExecuteCommand(new AddGoldCommand(50), currentState);

// Undo/Redo
if (commandHistory.CanUndo)
    var undoState = commandHistory.Undo(currentState);

if (commandHistory.CanRedo)
    var redoState = commandHistory.Redo(currentState);
```

### State Persistence
Save and load state with multiple backends:

```csharp
// File-based persistence
var persistence = new FileStatePersistence<GameState>();
persistence.SaveState(gameState, "savegame");
var loadedState = persistence.LoadState("savegame");

// PlayerPrefs persistence
var prefsPersistence = new PlayerPrefsStatePersistence<GameState>();
```

## 📋 Reactive Collections (v1.2.0)

### ObservableList
List with granular change tracking:

```csharp
var inventory = new ObservableList<string>();

// Subscribe to all changes
inventory.Subscribe(changeSet =>
    Debug.Log($"Inventory changed: {changeSet.GetSummary()}"));

// Subscribe to specific operations
inventory.ObserveAdd().Subscribe(addEvent =>
    Debug.Log($"Added {addEvent.Value} at index {addEvent.Index}"));

// Batch operations
inventory.AddRange(new[] { "Sword", "Shield", "Potion" });
```

### ObservableDictionary
Dictionary with key/value change notifications:

```csharp
var playerStats = new ObservableDictionary<string, int>();

// Subscribe to changes
playerStats.ObserveChanges().Subscribe(change =>
    Debug.Log($"Stat {change.Key}: {change.OldValue} -> {change.NewValue}"));

// Update stats
playerStats["Strength"] = 15;
playerStats["Agility"] = 12;
```

### ObservableSet
Set with add/remove notifications:

```csharp
var achievements = new ObservableSet<string>();

// Subscribe to additions
achievements.ObserveAdd().Subscribe(addEvent =>
    ShowAchievementUnlocked(addEvent.Item));

// Set operations
achievements.UnionWith(new[] { "First Steps", "Level Master" });
```

### Collection Synchronization
Synchronize multiple collections with conflict resolution:

```csharp
var synchronizer = new CollectionSynchronizer<string>(
    ConflictResolutionStrategy.SourceWins);

// Add collections to sync group
synchronizer.AddCollection(primaryInventory, "Primary");
synchronizer.AddCollection(backupInventory, "Backup");

// Changes to one collection automatically sync to others
primaryInventory.Add("New Item"); // Also appears in backupInventory
```

## 🎮 Unity-Specific Features

### Automatic Disposal
```csharp
observable.Subscribe(value => DoSomething(value))
          .AddTo(this); // Disposed when GameObject is destroyed
```

### Lifecycle Observables
```csharp
this.OnDestroyAsObservable()
    .Subscribe(_ => SaveGame());

this.UpdateAsObservable()
    .Subscribe(_ => UpdateLogic());
```

### UI Event Binding
```csharp
button.OnClickAsObservable()
inputField.OnValueChangedAsObservable()
slider.OnValueChangedAsObservable()
```

### Coroutine Integration
```csharp
Observable.FromCoroutine(() => LoadDataCoroutine())
    .Subscribe(_ => OnDataLoaded());
```

## 🔧 Operators

### Transformation
- `Select` / `Map` - Transform values (universal terminology support)
- `SelectMany` / `FlatMap` - Flatten nested observables (universal terminology support)
- `Buffer` - Collect values over time
- `Scan` / `Fold` - Accumulate values (universal terminology support)
- `Aggregate` / `Reduce` - Apply accumulator and return final result (universal terminology support)
- `AsUnitObservable` / `Void` - Convert to Unit observable (universal terminology support)

### Universal Reactive Programming Terminology
Ludo.Reactive supports familiar terminology from other reactive ecosystems:
- **Map** - Alias for Select (RxJS, RxJava compatible)
- **FlatMap** - Alias for SelectMany (RxJS, RxJava compatible)
- **Fold** - Alias for Scan (functional programming compatible)
- **Filter** - Alias for Where (RxJS, RxJava, functional programming compatible)
- **Tap** - Alias for Do (RxJS, RxJava, Angular compatible)
- **Reduce** - Alias for Aggregate (RxJS, JavaScript, functional programming compatible)
- **Of** - Alias for Return (RxJS, RxJava, RxSwift compatible)
- **Debounce** - Alias for Throttle (RxJS, web development, Angular compatible)
- **Distinct** - Alias for DistinctUntilChanged (RxJava, simplified terminology)
- **Void** - Alias for AsUnitObservable (RxSwift, RxJava compatible)

### Filtering
- `Where` / `Filter` - Filter by predicate (universal terminology support)
- `DistinctUntilChanged` / `Distinct` - Skip duplicate consecutive values (universal terminology support)
- `Throttle` / `Debounce` - Limit emission rate (universal terminology support)
- `Take` - Take first N values

### Combination
- `CombineLatest` - Combine latest values from multiple sources
- `Merge` - Merge multiple streams
- `Zip` - Pair values by index

### Utility
- `Do` / `Tap` - Side effects (universal terminology support)
- `Delay` - Time-shift emissions
- `Timeout` - Apply timeout policy

## 📊 Performance Features

- **Object Pooling**: Reduces garbage collection for frequently allocated objects
- **Dictionary Lookups**: O(1) subscription performance
- **Weak References**: Prevents memory leaks
- **Dirty Checking**: Optimized change detection
- **Lazy Evaluation**: Computations only when needed

## 🎯 Use Cases

### Game State Management
```csharp
// Health system with automatic UI updates
player.Health
    .Where(health => health > 0)
    .Select(health => health / maxHealth)
    .Subscribe(ratio => healthBar.fillAmount = ratio)
    .AddTo(this);
```

### Input Handling
```csharp
// Combo detection
inputStream
    .Buffer(3, 1)
    .Where(keys => keys.SequenceEqual(comboSequence))
    .Subscribe(_ => ExecuteCombo())
    .AddTo(this);
```

### Animation & Effects
```csharp
// Damage number batching
damageStream
    .Buffer(TimeSpan.FromMilliseconds(100))
    .Where(damages => damages.Count > 0)
    .Subscribe(damages => ShowCombinedDamage(damages.Sum()))
    .AddTo(this);
```

### Networking
```csharp
// Network message handling with throttling
networkMessages
    .OfType<PlayerMoveMessage>()
    .Throttle(TimeSpan.FromMilliseconds(50))
    .Subscribe(msg => UpdatePlayerPosition(msg))
    .AddTo(this);
```

## 📚 Documentation

- [Quick Start Guide](Documentation/Quick%20Start%20Guide.md) - Get up and running quickly
- [Architecture Design](Documentation/Architecture%20Design.md) - Detailed system design
- [API Reference](Documentation/API%20Reference.md) - Complete API documentation
- [Examples](Documentation/Examples.md) - Practical usage examples
- [Performance Guide](Documentation/Performance%20Guide.md) - Optimization techniques

## 🔧 Requirements

- Unity 6000.0 or later
- .NET Standard 2.1

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues and enhancement requests.

## 📞 Support

For questions and support, please refer to the documentation or create an issue in the repository.

---

**Ludo.Reactive** - Reactive programming made simple for Unity developers.
