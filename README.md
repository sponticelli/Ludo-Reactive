# Ludo.Reactive

A high-performance reactive programming framework designed specifically for Unity 6, providing developers with powerful tools for handling asynchronous data streams, event-driven programming, and automatic UI binding.

## ✨ Features

- **🎯 Unity-First Design**: Seamless integration with Unity's component system, lifecycle, and editor
- **⚡ High Performance**: Minimal garbage collection, optimized for mobile/WebGL platforms
- **🔧 Developer-Friendly**: Intuitive fluent API with comprehensive operator library
- **🏗️ SOLID Architecture**: Maintainable, extensible design following clean code principles
- **🔄 Automatic Memory Management**: Built-in disposal tracking with Unity lifecycle integration
- **📱 Platform Optimized**: WebGL and mobile-ready with object pooling and efficient algorithms

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
- `Select` - Transform values
- `SelectMany` - Flatten nested observables
- `Buffer` - Collect values over time
- `Scan` - Accumulate values

### Filtering
- `Where` - Filter by predicate
- `DistinctUntilChanged` - Skip duplicate consecutive values
- `Throttle` - Limit emission rate
- `Take` - Take first N values

### Combination
- `CombineLatest` - Combine latest values from multiple sources
- `Merge` - Merge multiple streams
- `Zip` - Pair values by index

### Utility
- `Do` - Side effects
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
