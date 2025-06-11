# Reactive Programming Concepts in Ludo.Reactive

This document explains the core concepts of reactive programming as implemented in Ludo.Reactive, helping you understand the mental model and design principles behind the framework.

## What is Reactive Programming?

Reactive programming is a programming paradigm that deals with data flows and the propagation of change. In reactive programming, you express static or dynamic data flows with ease, and the underlying execution model automatically propagates changes through the data flow.

### Traditional Imperative Approach

```csharp
// Traditional approach - manual updates
public class PlayerStats
{
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public float HealthPercentage { get; private set; }
    
    public void UpdateHealth(int newHealth)
    {
        Health = newHealth;
        HealthPercentage = (float)Health / MaxHealth; // Manual calculation
        OnHealthChanged?.Invoke(HealthPercentage); // Manual notification
    }
}
```

### Reactive Approach

```csharp
// Reactive approach - automatic updates
public class ReactivePlayerStats : ReactiveMonoBehaviour
{
    private ReactiveState<int> health;
    private ReactiveState<int> maxHealth;
    private ComputedValue<float> healthPercentage;
    
    protected override void InitializeReactive()
    {
        health = CreateState(100);
        maxHealth = CreateState(100);
        
        // Automatically recalculates when health or maxHealth changes
        healthPercentage = CreateComputed("healthPercentage", builder =>
        {
            var h = builder.Track(health);
            var maxH = builder.Track(maxHealth);
            return (float)h / maxH;
        });
    }
}
```

## Core Concepts

### 1. Reactive State

**ReactiveState<T>** represents a mutable value that can change over time and notifies observers when it changes.

```csharp
var counter = ReactiveFlow.CreateState(0);
counter.Set(5); // Notifies all observers
counter.Update(x => x + 1); // Functional update
```

**Key Properties:**
- **Mutable**: Can be changed using `Set()` or `Update()`
- **Observable**: Notifies when value changes
- **Equality-aware**: Only notifies if the new value is different

### 2. Computed Values

**ComputedValue<T>** represents a value derived from other reactive values. It automatically recalculates when its dependencies change.

```csharp
var firstName = ReactiveFlow.CreateState("John");
var lastName = ReactiveFlow.CreateState("Doe");

var fullName = ReactiveFlow.CreateComputed("fullName", builder =>
{
    return $"{builder.Track(firstName)} {builder.Track(lastName)}";
});

firstName.Set("Jane"); // fullName automatically becomes "Jane Doe"
```

**Key Properties:**
- **Derived**: Value is computed from other reactive values
- **Cached**: Result is cached until dependencies change
- **Lazy**: Only recalculates when accessed and dependencies have changed
- **Pure**: Should not have side effects

### 3. Effects

**ReactiveEffect** represents side effects that should run when reactive values change.

```csharp
var playerHealth = ReactiveFlow.CreateState(100);

var healthLogger = ReactiveFlow.CreateEffect("healthLogger", builder =>
{
    var health = builder.Track(playerHealth);
    Debug.Log($"Player health: {health}");
});

playerHealth.Set(80); // Logs: "Player health: 80"
```

**Key Properties:**
- **Side effects**: Can perform I/O, update UI, etc.
- **Reactive**: Runs when dependencies change
- **Asynchronous**: Execution is batched and deferred

### 4. Dependency Tracking

Ludo.Reactive uses automatic dependency tracking. When you call `builder.Track()` inside a computation, the framework automatically registers that reactive value as a dependency.

```csharp
var a = ReactiveFlow.CreateState(1);
var b = ReactiveFlow.CreateState(2);
var condition = ReactiveFlow.CreateState(true);

var result = ReactiveFlow.CreateComputed("conditional", builder =>
{
    if (builder.Track(condition))
        return builder.Track(a); // 'a' is tracked only when condition is true
    else
        return builder.Track(b); // 'b' is tracked only when condition is false
});
```

**Dynamic Dependencies**: Dependencies can change based on the execution path, and the framework adapts automatically.

### 5. Batched Updates

Ludo.Reactive batches updates to prevent unnecessary recalculations:

```csharp
var x = ReactiveFlow.CreateState(1);
var y = ReactiveFlow.CreateState(2);
var sum = ReactiveFlow.CreateComputed("sum", builder => 
    builder.Track(x) + builder.Track(y));

// Without batching, this would trigger sum calculation twice
ReactiveFlow.ExecuteBatch(() =>
{
    x.Set(10); // sum doesn't recalculate yet
    y.Set(20); // sum doesn't recalculate yet
}); // sum recalculates once here with final values
```

## Mental Model

Think of reactive programming as a spreadsheet:

1. **Cells with values** = ReactiveState
2. **Cells with formulas** = ComputedValue
3. **Conditional formatting** = ReactiveEffect
4. **Automatic recalculation** = Dependency tracking

When you change a cell value, all dependent formulas automatically recalculate, just like in a spreadsheet.

## Data Flow

```
ReactiveState ──┐
                ├─→ ComputedValue ──┐
ReactiveState ──┘                   ├─→ ReactiveEffect
                                    │
Other Dependencies ─────────────────┘
```

1. **Sources**: ReactiveState values are the sources of truth
2. **Transformations**: ComputedValues transform and combine sources
3. **Sinks**: ReactiveEffects consume values and perform side effects

## Benefits

### Declarative Code
Express **what** the UI should look like, not **how** to update it:

```csharp
// Declarative: "Health bar fill should be health/maxHealth"
CreateEffect(builder =>
{
    var health = builder.Track(playerHealth);
    var maxHealth = builder.Track(playerMaxHealth);
    healthBar.fillAmount = (float)health / maxHealth;
});
```

### Automatic Consistency
No need to remember to update dependent values manually:

```csharp
// All these automatically stay in sync
var level = CreateState(1);
var experience = CreateState(0);
var experienceToNext = CreateComputed("expToNext", builder => 
    (builder.Track(level) * 100) - builder.Track(experience));
var progressBar = CreateComputed("progress", builder =>
    builder.Track(experience) / (float)(builder.Track(level) * 100));
```

### Composability
Reactive values can be easily composed and reused:

```csharp
var health = CreateState(100);
var maxHealth = CreateState(100);
var healthPercent = CreateComputed("healthPercent", builder =>
    builder.Track(health) / (float)builder.Track(maxHealth));

// Reuse healthPercent in multiple places
var healthBarFill = CreateComputed("healthBarFill", builder => builder.Track(healthPercent));
var healthColor = CreateComputed("healthColor", builder =>
{
    var percent = builder.Track(healthPercent);
    return percent > 0.5f ? Color.green : percent > 0.25f ? Color.yellow : Color.red;
});
```

## Common Patterns

### State Machines
```csharp
var gameState = CreateState(GameState.Menu);
var canStartGame = CreateComputed("canStart", builder => 
    builder.Track(gameState) == GameState.Menu);
var isPlaying = CreateComputed("isPlaying", builder => 
    builder.Track(gameState) == GameState.Playing);
```

### Validation
```csharp
var email = CreateState("");
var isValidEmail = CreateComputed("isValidEmail", builder =>
    builder.Track(email).Contains("@"));
```

### Derived Collections
```csharp
var allItems = CreateState(new List<Item>());
var visibleItems = CreateComputed("visibleItems", builder =>
    builder.Track(allItems).Where(item => item.IsVisible).ToList());
```

## Next Steps

- Learn about the [API Reference](API-Reference.md) for detailed method documentation
- See [Pure C# Usage](Pure-CSharp-Usage.md) for non-Unity examples
- Explore [Unity Integration](Unity-Integration.md) for Unity-specific features
- Check [Patterns and Anti-Patterns](Patterns-and-AntiPatterns.md) for best practices
