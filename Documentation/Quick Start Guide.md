# Ludo.Reactive - Quick Start Guide

## Overview

Ludo.Reactive is a powerful reactive programming framework designed specifically for Unity 6. It provides developers with intuitive tools for handling asynchronous data streams, event-driven programming, and automatic UI binding.

## Installation

The package is already included in your Unity project. Simply add the `Ludo.Reactive` namespace to your scripts:

```csharp
using Ludo.Reactive;
```

## Core Concepts

### ReactiveProperty<T>

A `ReactiveProperty<T>` is a container that holds a value and notifies observers when the value changes.

```csharp
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private ReactiveProperty<int> _health = new ReactiveProperty<int>(100);
    
    public IReadOnlyReactiveProperty<int> Health => _health.AsReadOnly();
    
    void Start()
    {
        // Subscribe to health changes
        _health.Subscribe(health => Debug.Log($"Health: {health}"))
               .AddTo(this); // Automatically dispose when GameObject is destroyed
    }
    
    public void TakeDamage(int damage)
    {
        _health.Value -= damage; // This will notify all subscribers
    }
}
```

### Observable Sequences

Create and manipulate streams of data using observable sequences:

```csharp
void Start()
{
    // Create an observable that emits every second
    Observable.Interval(TimeSpan.FromSeconds(1))
        .Take(10) // Only take first 10 values
        .Subscribe(count => Debug.Log($"Count: {count}"))
        .AddTo(this);
    
    // Transform data streams
    _health
        .Where(health => health > 0) // Filter values
        .Select(health => health / 100f) // Transform to percentage
        .Subscribe(percentage => healthBar.fillAmount = percentage)
        .AddTo(this);
}
```

### Unity Integration

Ludo.Reactive seamlessly integrates with Unity components and events:

```csharp
void Start()
{
    // Observe Unity lifecycle events
    this.OnDestroyAsObservable()
        .Subscribe(_ => Debug.Log("GameObject destroyed"))
        .AddTo(this);
    
    // Observe UI events
    button.OnClickAsObservable()
        .Subscribe(_ => Debug.Log("Button clicked"))
        .AddTo(this);
    
    // Observe Unity's Update loop
    this.UpdateAsObservable()
        .Where(_ => Input.GetKeyDown(KeyCode.Space))
        .Subscribe(_ => Jump())
        .AddTo(this);
}
```

## Common Patterns

### Health System with UI Binding

```csharp
public class HealthSystem : MonoBehaviour
{
    [SerializeField] private ReactiveProperty<float> _currentHealth = new(100f);
    [SerializeField] private ReactiveProperty<float> _maxHealth = new(100f);
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthText;
    
    void Start()
    {
        // Bind health to UI
        var healthPercentage = _currentHealth
            .CombineLatest(_maxHealth, (current, max) => current / max);
        
        healthPercentage
            .Subscribe(percentage => healthSlider.value = percentage)
            .AddTo(this);
        
        _currentHealth
            .Subscribe(health => healthText.text = $"Health: {health:F0}")
            .AddTo(this);
        
        // Death detection
        _currentHealth
            .Where(health => health <= 0)
            .Take(1)
            .Subscribe(_ => OnDeath())
            .AddTo(this);
    }
}
```

### Input Handling with Throttling

```csharp
void Start()
{
    // Prevent button spam
    fireButton.OnClickAsObservable()
        .Throttle(TimeSpan.FromSeconds(0.5f))
        .Subscribe(_ => Fire())
        .AddTo(this);
    
    // Combo detection
    var inputStream = this.UpdateAsObservable()
        .Where(_ => Input.anyKeyDown)
        .Select(_ => GetPressedKey());
    
    inputStream
        .Buffer(3, 1) // Look at last 3 inputs
        .Where(keys => keys.SequenceEqual(new[] { KeyCode.A, KeyCode.B, KeyCode.C }))
        .Subscribe(_ => ExecuteCombo())
        .AddTo(this);
}
```

### Async Operations

```csharp
void Start()
{
    // Convert coroutines to observables
    Observable.FromCoroutine(() => LoadDataCoroutine())
        .Subscribe(_ => Debug.Log("Data loaded"))
        .AddTo(this);
    
    // Handle multiple async operations
    var loadPlayer = LoadPlayerData();
    var loadLevel = LoadLevelData();
    
    loadPlayer.CombineLatest(loadLevel, (player, level) => new { player, level })
        .Subscribe(data => StartGame(data.player, data.level))
        .AddTo(this);
}
```

## Best Practices

### 1. Always Use AddTo()

Always call `.AddTo(this)` on subscriptions to prevent memory leaks:

```csharp
observable.Subscribe(value => DoSomething(value))
          .AddTo(this); // ✅ Good
          
observable.Subscribe(value => DoSomething(value)); // ❌ Memory leak
```

### 2. Use Read-Only Properties for Public Access

Expose reactive properties as read-only to prevent external modification:

```csharp
public class Player : MonoBehaviour
{
    [SerializeField] private ReactiveProperty<int> _score = new(0);
    
    public IReadOnlyReactiveProperty<int> Score => _score.AsReadOnly(); // ✅ Good
    public ReactiveProperty<int> Score => _score; // ❌ Allows external modification
}
```

### 3. Use Operators for Data Transformation

Leverage operators instead of manual state management:

```csharp
// ✅ Good - Declarative
var isLowHealth = health
    .Select(h => h < 20)
    .DistinctUntilChanged();

// ❌ Bad - Imperative
bool isLowHealth = false;
health.Subscribe(h => 
{
    var newIsLowHealth = h < 20;
    if (newIsLowHealth != isLowHealth)
    {
        isLowHealth = newIsLowHealth;
        // Handle change...
    }
});
```

### 4. Combine Related Data

Use `CombineLatest` for dependent data:

```csharp
var canCastSpell = mana
    .CombineLatest(spellCost, (current, cost) => current >= cost)
    .DistinctUntilChanged();

canCastSpell.Subscribe(canCast => spellButton.interactable = canCast)
           .AddTo(this);
```

## Performance Tips

1. **Use DistinctUntilChanged()** to prevent unnecessary updates
2. **Use Throttle()** for high-frequency events like mouse movement
3. **Use Where()** to filter early in the chain
4. **Dispose subscriptions** when no longer needed (AddTo handles this automatically)

## Next Steps

- Explore the [API Reference](API%20Reference.md) for detailed documentation
- Check out more [Examples](Examples.md) for advanced patterns
- Read about [Performance Optimization](Performance%20Guide.md) techniques
- Learn about [Testing](Testing%20Guide.md) reactive code

## Common Issues

### Memory Leaks
**Problem**: Subscriptions not disposed
**Solution**: Always use `.AddTo(this)` or manually dispose subscriptions

### Performance Issues
**Problem**: Too many notifications
**Solution**: Use `DistinctUntilChanged()`, `Throttle()`, or `Where()` to reduce notifications

### Null Reference Exceptions
**Problem**: Accessing disposed reactive properties
**Solution**: Check for null and use proper lifecycle management

For more detailed information, see the complete documentation in the Documentation folder.
