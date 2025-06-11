# Ludo.Reactive

A high-performance reactive programming framework for C# and Unity 6 with automatic dependency tracking, declarative state management, and efficient batched updates.

## Quick Start

```csharp
using Ludo.Reactive;

// Create reactive state
var counter = ReactiveFlow.CreateState(0);
var doubled = ReactiveFlow.CreateComputed("doubled", builder =>
    builder.Track(counter) * 2);

// Create effect that runs when values change
var logger = ReactiveFlow.CreateEffect("logger", builder =>
    Console.WriteLine($"Counter: {builder.Track(counter)}, Doubled: {builder.Track(doubled)}"));

// Update state - automatically triggers recalculation
counter.Set(5); // Prints: "Counter: 5, Doubled: 10"

// Cleanup
logger.Dispose();
doubled.Dispose();
```

## Unity Integration

```csharp
using Ludo.Reactive.Unity;

public class PlayerHealth : ReactiveMonoBehaviour
{
    private ReactiveState<int> health;
    private ComputedValue<bool> isAlive;

    protected override void InitializeReactive()
    {
        health = CreateState(100);
        isAlive = CreateComputed(builder => builder.Track(health) > 0);

        CreateEffect(builder =>
        {
            if (!builder.Track(isAlive))
                Debug.Log("Player died!");
        });
    }

    public void TakeDamage(int damage) => health.Update(h => Mathf.Max(0, h - damage));
}
```

## Key Features

- 🚀 **High Performance**: Efficient batched updates and minimal memory overhead
- 🎯 **Declarative**: Express what your UI should look like based on state
- 🔄 **Automatic Dependency Tracking**: No manual subscription management
- 🧵 **Thread-Safe**: Safe for concurrent access with Unity main thread integration
- 🎮 **Unity Integration**: Seamless MonoBehaviour integration and reactive UI components

## Documentation

📚 **[Complete Documentation](Documentation/README.md)**

- [Core Concepts](Documentation/Concepts.md) - Understanding reactive programming
- [API Reference](Documentation/API-Reference.md) - Detailed API documentation
- [Pure C# Usage](Documentation/Pure-CSharp-Usage.md) - Using without Unity
- [Unity Integration](Documentation/Unity-Integration.md) - Unity-specific features
- [Examples and Use Cases](Documentation/Examples-and-UseCases.md) - Real-world scenarios
- [Patterns and Anti-Patterns](Documentation/Patterns-and-AntiPatterns.md) - Best practices

## Installation

### Unity Package Manager
1. Open Unity Package Manager
2. Click "+" and select "Add package from git URL"
3. Enter: `https://github.com/sponticelli/Ludo-Reactive.git`

### Manual Installation
1. Download the latest release
2. Import the package into your Unity project

## Requirements

- Unity 6000.0 or later
- .NET Standard 2.1

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📖 [Documentation](Documentation/README.md)
- 🐛 [Issues](https://github.com/sponticelli/Ludo-Reactive/issues)
- 💬 [Discussions](https://github.com/sponticelli/Ludo-Reactive/discussions)
