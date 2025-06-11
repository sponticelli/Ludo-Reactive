# Patterns and Anti-Patterns

This guide covers best practices, recommended patterns, and common mistakes to avoid when using Ludo.Reactive.

## Table of Contents

- [Best Practices](#best-practices)
- [Recommended Patterns](#recommended-patterns)
- [Common Anti-Patterns](#common-anti-patterns)
- [Performance Patterns](#performance-patterns)
- [Testing Patterns](#testing-patterns)
- [Memory Management](#memory-management)

## Best Practices

### 1. Use Descriptive Names

**✅ Good:**
```csharp
var playerHealth = ReactiveFlow.CreateState(100);
var isPlayerAlive = ReactiveFlow.CreateComputed("isPlayerAlive", builder =>
    builder.Track(playerHealth) > 0);
var healthRegenerationEffect = ReactiveFlow.CreateEffect("healthRegen", builder =>
{
    if (builder.Track(isPlayerAlive))
    {
        // Regenerate health logic
    }
});
```

**❌ Bad:**
```csharp
var h = ReactiveFlow.CreateState(100);
var c = ReactiveFlow.CreateComputed("c", builder => builder.Track(h) > 0);
var e = ReactiveFlow.CreateEffect("e", builder => { /* logic */ });
```

### 2. Keep Computations Pure

**✅ Good:**
```csharp
var totalScore = ReactiveFlow.CreateComputed("totalScore", builder =>
{
    var baseScore = builder.Track(playerScore);
    var multiplier = builder.Track(scoreMultiplier);
    return baseScore * multiplier; // Pure calculation
});
```

**❌ Bad:**
```csharp
var totalScore = ReactiveFlow.CreateComputed("totalScore", builder =>
{
    var baseScore = builder.Track(playerScore);
    Debug.Log("Calculating score..."); // Side effect in computation!
    SaveScoreToFile(baseScore); // Side effect in computation!
    return baseScore * 2;
});
```

### 3. Use Effects for Side Effects

**✅ Good:**
```csharp
var playerScore = ReactiveFlow.CreateState(0);

// Pure computation
var scoreDisplay = ReactiveFlow.CreateComputed("scoreDisplay", builder =>
    $"Score: {builder.Track(playerScore):N0}");

// Side effect in effect
var scoreLogger = ReactiveFlow.CreateEffect("scoreLogger", builder =>
{
    var score = builder.Track(playerScore);
    Debug.Log($"Player score updated: {score}");
    SaveScoreToFile(score);
});
```

### 4. Batch Related Updates

**✅ Good:**
```csharp
public void LevelUp()
{
    ReactiveFlow.ExecuteBatch(() =>
    {
        playerLevel.Set(playerLevel.Current + 1);
        playerExperience.Set(0);
        playerHealth.Set(playerMaxHealth.Current);
        playerMana.Set(playerMaxMana.Current);
    }); // All dependent computations run once after this
}
```

**❌ Bad:**
```csharp
public void LevelUp()
{
    playerLevel.Set(playerLevel.Current + 1); // Triggers recalculations
    playerExperience.Set(0); // Triggers recalculations
    playerHealth.Set(playerMaxHealth.Current); // Triggers recalculations
    playerMana.Set(playerMaxMana.Current); // Triggers recalculations
}
```

### 5. Dispose Resources Properly

**✅ Good:**
```csharp
public class GameManager : ReactiveMonoBehaviour
{
    private ComputedValue<bool> gameWon;
    private ReactiveEffect winEffect;
    
    protected override void InitializeReactive()
    {
        gameWon = CreateComputed(builder => /* logic */); // Auto-disposed
        winEffect = CreateEffect(builder => /* logic */); // Auto-disposed
    }
    
    // ReactiveMonoBehaviour handles disposal automatically
}
```

**✅ Also Good (Manual Management):**
```csharp
public class GameManager : IDisposable
{
    private readonly List<IDisposable> disposables = new();
    
    public GameManager()
    {
        var gameWon = ReactiveFlow.CreateComputed("gameWon", builder => /* logic */);
        var winEffect = ReactiveFlow.CreateEffect("winEffect", builder => /* logic */);
        
        disposables.Add(gameWon);
        disposables.Add(winEffect);
    }
    
    public void Dispose()
    {
        foreach (var disposable in disposables)
            disposable?.Dispose();
        disposables.Clear();
    }
}
```

## Recommended Patterns

### 1. State Machine Pattern

```csharp
public enum GameState { Menu, Playing, Paused, GameOver }

public class GameStateMachine : ReactiveMonoBehaviour
{
    private ReactiveState<GameState> currentState;
    private ComputedValue<bool> canPause;
    private ComputedValue<bool> canResume;
    private ComputedValue<bool> canRestart;
    
    protected override void InitializeReactive()
    {
        currentState = CreateState(GameState.Menu);
        
        canPause = CreateComputed(builder =>
            builder.Track(currentState) == GameState.Playing);
        
        canResume = CreateComputed(builder =>
            builder.Track(currentState) == GameState.Paused);
        
        canRestart = CreateComputed(builder =>
        {
            var state = builder.Track(currentState);
            return state == GameState.GameOver || state == GameState.Paused;
        });
        
        // State transition effects
        CreateEffect(builder =>
        {
            var state = builder.Track(currentState);
            Time.timeScale = state == GameState.Playing ? 1f : 0f;
        });
    }
    
    public void Pause()
    {
        if (canPause.Current)
            currentState.Set(GameState.Paused);
    }
    
    public void Resume()
    {
        if (canResume.Current)
            currentState.Set(GameState.Playing);
    }
    
    public void StartGame() => currentState.Set(GameState.Playing);
    public void EndGame() => currentState.Set(GameState.GameOver);
    public void ReturnToMenu() => currentState.Set(GameState.Menu);
}
```

### 2. Validation Pattern

```csharp
public class FormValidator : ReactiveMonoBehaviour
{
    private ReactiveState<string> email;
    private ReactiveState<string> password;
    
    private ComputedValue<ValidationResult> emailValidation;
    private ComputedValue<ValidationResult> passwordValidation;
    private ComputedValue<bool> isFormValid;
    
    protected override void InitializeReactive()
    {
        email = CreateState("");
        password = CreateState("");
        
        emailValidation = CreateComputed(builder =>
        {
            var value = builder.Track(email);
            if (string.IsNullOrEmpty(value))
                return new ValidationResult(false, "Email is required");
            if (!IsValidEmail(value))
                return new ValidationResult(false, "Invalid email format");
            return new ValidationResult(true, "");
        });
        
        passwordValidation = CreateComputed(builder =>
        {
            var value = builder.Track(password);
            if (string.IsNullOrEmpty(value))
                return new ValidationResult(false, "Password is required");
            if (value.Length < 8)
                return new ValidationResult(false, "Password must be at least 8 characters");
            return new ValidationResult(true, "");
        });
        
        isFormValid = CreateComputed(builder =>
            builder.Track(emailValidation).IsValid &&
            builder.Track(passwordValidation).IsValid);
    }
    
    private bool IsValidEmail(string email) => email.Contains("@");
}

public struct ValidationResult
{
    public bool IsValid { get; }
    public string Message { get; }
    
    public ValidationResult(bool isValid, string message)
    {
        IsValid = isValid;
        Message = message;
    }
}
```

### 3. Repository Pattern with Reactive Data

```csharp
public class PlayerDataRepository : ReactiveMonoBehaviour
{
    private ReactiveState<PlayerData> playerData;
    private ReactiveState<bool> isLoading;
    private ReactiveState<bool> isDirty;
    
    private ComputedValue<bool> canSave;
    private ComputedValue<string> saveStatus;
    
    protected override void InitializeReactive()
    {
        playerData = CreateState(new PlayerData());
        isLoading = CreateState(false);
        isDirty = CreateState(false);
        
        canSave = CreateComputed(builder =>
            builder.Track(isDirty) && !builder.Track(isLoading));
        
        saveStatus = CreateComputed(builder =>
        {
            var loading = builder.Track(isLoading);
            var dirty = builder.Track(isDirty);
            
            if (loading) return "Saving...";
            if (dirty) return "Unsaved changes";
            return "All changes saved";
        });
        
        // Auto-save effect
        CreateEffect(builder =>
        {
            if (builder.Track(canSave))
            {
                // Debounce auto-save
                StartCoroutine(AutoSaveCoroutine());
            }
        });
    }
    
    public void UpdatePlayerData(PlayerData newData)
    {
        playerData.Set(newData);
        isDirty.Set(true);
    }
    
    public async Task SaveAsync()
    {
        if (!canSave.Current) return;
        
        isLoading.Set(true);
        try
        {
            await SaveToServer(playerData.Current);
            isDirty.Set(false);
        }
        finally
        {
            isLoading.Set(false);
        }
    }
    
    private async Task SaveToServer(PlayerData data)
    {
        // Simulate network call
        await Task.Delay(1000);
    }
    
    private System.Collections.IEnumerator AutoSaveCoroutine()
    {
        yield return new WaitForSeconds(5f); // 5 second debounce
        if (canSave.Current)
        {
            _ = SaveAsync();
        }
    }
}
```

## Common Anti-Patterns

### 1. ❌ Circular Dependencies

**Problem:**
```csharp
var a = ReactiveFlow.CreateState(1);
var b = ReactiveFlow.CreateComputed("b", builder => builder.Track(a) + 1);
var c = ReactiveFlow.CreateComputed("c", builder => builder.Track(b) + 1);

// This creates a circular dependency!
var badEffect = ReactiveFlow.CreateEffect("bad", builder =>
{
    var value = builder.Track(c);
    a.Set(value); // This will cause infinite loop!
});
```

**Solution:**
```csharp
var a = ReactiveFlow.CreateState(1);
var b = ReactiveFlow.CreateComputed("b", builder => builder.Track(a) + 1);
var c = ReactiveFlow.CreateComputed("c", builder => builder.Track(b) + 1);

// Use a separate trigger or condition to break the cycle
var shouldUpdate = ReactiveFlow.CreateState(false);
var updateEffect = ReactiveFlow.CreateEffect("update", builder =>
{
    if (builder.Track(shouldUpdate))
    {
        var value = builder.Track(c);
        a.Set(value);
        shouldUpdate.Set(false); // Break the cycle
    }
});
```

### 2. ❌ Heavy Computations in Reactive Values

**Problem:**
```csharp
var expensiveComputation = ReactiveFlow.CreateComputed("expensive", builder =>
{
    var data = builder.Track(largeDataSet);
    
    // This runs every time largeDataSet changes!
    return data.Where(x => x.IsValid)
               .Select(x => ComplexCalculation(x))
               .OrderBy(x => x.Score)
               .ToList();
});
```

**Solution:**
```csharp
// Use memoization or caching
private readonly Dictionary<int, List<ProcessedData>> cache = new();

var optimizedComputation = ReactiveFlow.CreateComputed("optimized", builder =>
{
    var data = builder.Track(largeDataSet);
    var hash = data.GetHashCode();
    
    if (!cache.ContainsKey(hash))
    {
        cache[hash] = data.Where(x => x.IsValid)
                          .Select(x => ComplexCalculation(x))
                          .OrderBy(x => x.Score)
                          .ToList();
    }
    
    return cache[hash];
});
```

### 3. ❌ Forgetting to Dispose

**Problem:**
```csharp
public class BadManager
{
    public void CreateTemporaryEffect()
    {
        // This effect will never be disposed!
        var effect = ReactiveFlow.CreateEffect("temp", builder =>
        {
            // Some logic
        });
        
        // Effect continues to run even after this method ends
    }
}
```

**Solution:**
```csharp
public class GoodManager : IDisposable
{
    private readonly List<IDisposable> disposables = new();
    
    public void CreateTemporaryEffect()
    {
        var effect = ReactiveFlow.CreateEffect("temp", builder =>
        {
            // Some logic
        });
        
        disposables.Add(effect); // Track for disposal
    }
    
    public void Dispose()
    {
        foreach (var disposable in disposables)
            disposable?.Dispose();
        disposables.Clear();
    }
}
```

### 4. ❌ Mutating Reactive State in Computations

**Problem:**
```csharp
var counter = ReactiveFlow.CreateState(0);
var badComputed = ReactiveFlow.CreateComputed("bad", builder =>
{
    var value = builder.Track(counter);
    counter.Set(value + 1); // Mutation in computation!
    return value * 2;
});
```

**Solution:**
```csharp
var counter = ReactiveFlow.CreateState(0);
var doubled = ReactiveFlow.CreateComputed("doubled", builder =>
{
    var value = builder.Track(counter);
    return value * 2; // Pure computation
});

var incrementEffect = ReactiveFlow.CreateEffect("increment", builder =>
{
    var value = builder.Track(doubled);
    if (SomeCondition())
    {
        counter.Set(counter.Current + 1); // Mutation in effect
    }
});
```

## Performance Patterns

### 1. Debouncing Rapid Updates

```csharp
public class DebouncedSearch : ReactiveMonoBehaviour
{
    private ReactiveState<string> searchTerm;
    private ReactiveState<float> lastUpdateTime;
    private ComputedValue<bool> shouldSearch;
    
    protected override void InitializeReactive()
    {
        searchTerm = CreateState("");
        lastUpdateTime = CreateState(0f);
        
        shouldSearch = CreateComputed(builder =>
        {
            var term = builder.Track(searchTerm);
            var lastUpdate = builder.Track(lastUpdateTime);
            var currentTime = Time.time;
            
            return !string.IsNullOrEmpty(term) && 
                   (currentTime - lastUpdate) > 0.5f; // 500ms debounce
        });
        
        CreateEffect(builder =>
        {
            if (builder.Track(shouldSearch))
            {
                PerformSearch(searchTerm.Current);
            }
        });
    }
    
    public void UpdateSearchTerm(string term)
    {
        searchTerm.Set(term);
        lastUpdateTime.Set(Time.time);
    }
    
    private void PerformSearch(string term)
    {
        Debug.Log($"Searching for: {term}");
        // Perform actual search
    }
}
```

### 2. Conditional Computation

```csharp
var enableExpensiveCalculation = ReactiveFlow.CreateState(false);
var inputData = ReactiveFlow.CreateState(new List<int>());

var conditionalResult = ReactiveFlow.CreateComputed("conditional", builder =>
{
    if (!builder.Track(enableExpensiveCalculation))
        return new List<int>(); // Return early if disabled
    
    var data = builder.Track(inputData);
    return data.Where(x => IsPrime(x)).ToList(); // Expensive calculation
});
```

## Testing Patterns

### 1. Testing Reactive Logic

```csharp
[Test]
public void TestPlayerHealthCalculation()
{
    // Arrange
    var baseHealth = ReactiveFlow.CreateState(100);
    var healthModifier = ReactiveFlow.CreateState(1.0f);
    var finalHealth = ReactiveFlow.CreateComputed("finalHealth", builder =>
        (int)(builder.Track(baseHealth) * builder.Track(healthModifier)));
    
    // Act & Assert
    Assert.AreEqual(100, finalHealth.Current);
    
    healthModifier.Set(1.5f);
    Assert.AreEqual(150, finalHealth.Current);
    
    baseHealth.Set(80);
    Assert.AreEqual(120, finalHealth.Current);
    
    // Cleanup
    finalHealth.Dispose();
}
```

### 2. Testing Effects

```csharp
[Test]
public void TestEffectExecution()
{
    // Arrange
    var counter = 0;
    var trigger = ReactiveFlow.CreateState(false);
    var effect = ReactiveFlow.CreateEffect("test", builder =>
    {
        if (builder.Track(trigger))
            counter++;
    });
    
    // Act & Assert
    Assert.AreEqual(0, counter); // Effect runs once initially
    
    trigger.Set(true);
    Assert.AreEqual(1, counter);
    
    trigger.Set(false);
    Assert.AreEqual(1, counter); // No change
    
    trigger.Set(true);
    Assert.AreEqual(2, counter);
    
    // Cleanup
    effect.Dispose();
}
```

## Memory Management

### 1. Use Object Pooling for Frequent Allocations

```csharp
public class PooledReactiveList<T> : ReactiveMonoBehaviour
{
    private ReactiveState<List<T>> items;
    private readonly Queue<List<T>> listPool = new();
    
    protected override void InitializeReactive()
    {
        items = CreateState(GetPooledList());
    }
    
    private List<T> GetPooledList()
    {
        return listPool.Count > 0 ? listPool.Dequeue() : new List<T>();
    }
    
    private void ReturnToPool(List<T> list)
    {
        list.Clear();
        listPool.Enqueue(list);
    }
    
    public void UpdateItems(IEnumerable<T> newItems)
    {
        var oldList = items.Current;
        var newList = GetPooledList();
        newList.AddRange(newItems);
        
        items.Set(newList);
        ReturnToPool(oldList);
    }
}
```

### 2. Weak References for Large Objects

```csharp
public class WeakReactiveReference<T> where T : class
{
    private readonly ReactiveState<WeakReference<T>> weakRef;
    
    public WeakReactiveReference(T initialValue)
    {
        weakRef = ReactiveFlow.CreateState(new WeakReference<T>(initialValue));
    }
    
    public T Current
    {
        get
        {
            if (weakRef.Current.TryGetTarget(out var target))
                return target;
            return null;
        }
    }
    
    public void Set(T value)
    {
        weakRef.Set(new WeakReference<T>(value));
    }
}
```
