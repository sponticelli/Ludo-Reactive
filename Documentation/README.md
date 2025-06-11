# Ludo.Reactive Documentation

A high-performance reactive programming framework for C# and Unity 6 with automatic dependency tracking, declarative state management, and efficient batched updates.

## Table of Contents

- [Quick Start](#quick-start)
- [Core Concepts](Concepts.md)
- [API Reference](API-Reference.md)
- [Pure C# Usage](Pure-CSharp-Usage.md)
- [Unity Integration](Unity-Integration.md)
- [Examples and Use Cases](Examples-and-UseCases.md)
- [Patterns and Anti-Patterns](Patterns-and-AntiPatterns.md)

## Quick Start

### Installation

Add the package to your Unity project via the Package Manager using the Git URL or by importing the package directly.

### Basic Example

```csharp
using Ludo.Reactive;

// Create reactive state
var counter = ReactiveFlow.CreateState(0);
var multiplier = ReactiveFlow.CreateState(2);

// Create computed value that automatically updates
var doubled = ReactiveFlow.CreateComputed("doubled", builder =>
{
    return builder.Track(counter) * builder.Track(multiplier);
});

// Create effect that runs when values change
var effect = ReactiveFlow.CreateEffect("logger", builder =>
{
    var value = builder.Track(doubled);
    Console.WriteLine($"Doubled value: {value}");
});

// Update state - automatically triggers recalculation and effect
counter.Set(5); // Prints: "Doubled value: 10"
multiplier.Set(3); // Prints: "Doubled value: 15"

// Cleanup
effect.Dispose();
doubled.Dispose();
```

### Unity Example

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;

public class PlayerHealthUI : ReactiveMonoBehaviour
{
    [SerializeField] private ReactiveText healthText;
    [SerializeField] private ReactiveImage healthBar;
    
    private ReactiveState<int> health;
    private ReactiveState<int> maxHealth;
    
    protected override void InitializeReactive()
    {
        health = CreateState(100);
        maxHealth = CreateState(100);
        
        // Update health text
        CreateEffect(builder =>
        {
            var current = builder.Track(health);
            var max = builder.Track(maxHealth);
            healthText.SetText($"{current}/{max}");
        });
        
        // Update health bar fill
        CreateEffect(builder =>
        {
            var current = builder.Track(health);
            var max = builder.Track(maxHealth);
            var fillAmount = (float)current / max;
            healthBar.SetFillAmount(fillAmount);
        });
    }
    
    public void TakeDamage(int damage)
    {
        health.Update(current => Mathf.Max(0, current - damage));
    }
}
```

## Key Features

### 🚀 **High Performance**
- Efficient batched updates prevent unnecessary recalculations
- Automatic dependency tracking eliminates manual subscription management
- Memory-efficient subscription handling with automatic cleanup

### 🎯 **Declarative State Management**
- Express what your UI should look like based on state
- Automatic updates when dependencies change
- No manual event handling or callback management

### 🔄 **Automatic Dependency Tracking**
- Dependencies are tracked automatically during computation
- No need to manually specify what values a computation depends on
- Dynamic dependency tracking adapts to conditional logic

### 🧵 **Thread-Safe Design**
- Safe to use across multiple threads
- Unity integration ensures UI updates happen on main thread
- Proper synchronization for concurrent access

### 🎮 **Unity Integration**
- Seamless integration with Unity's component system
- Reactive UI components for common use cases
- Lifecycle management tied to GameObject lifecycle

## Architecture Overview

Ludo.Reactive is built around several core concepts:

- **ReactiveState<T>**: Mutable reactive values that notify when changed
- **ComputedValue<T>**: Derived values that automatically recalculate
- **ReactiveEffect**: Side effects that run when dependencies change
- **ComputationBuilder**: Provides dependency tracking during computation
- **ReactiveScheduler**: Manages batched execution of computations

## Performance Characteristics

- **Memory**: Minimal overhead per reactive value (~64 bytes)
- **CPU**: Batched updates prevent redundant calculations
- **Scalability**: Handles thousands of reactive values efficiently
- **GC Pressure**: Minimal allocations during normal operation

## Browser Compatibility

When using in Unity WebGL builds:
- Full feature compatibility
- Efficient execution in browser environments
- No additional dependencies required

## Next Steps

- Read [Core Concepts](Concepts.md) to understand reactive programming principles
- Check [API Reference](API-Reference.md) for detailed method documentation
- Explore [Examples and Use Cases](Examples-and-UseCases.md) for practical implementations
- Review [Patterns and Anti-Patterns](Patterns-and-AntiPatterns.md) for best practices

## Support

For questions, issues, or contributions, please visit the [GitHub repository](https://github.com/sponticelli/Ludo-Reactive).
