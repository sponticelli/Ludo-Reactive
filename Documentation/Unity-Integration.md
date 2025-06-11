# Unity Integration

This guide covers Unity-specific features and integration patterns for Ludo.Reactive.

## Unity-Specific Features

### UnityReactiveFlow

Unity-specific entry point that ensures computations run on the main thread.

```csharp
using Ludo.Reactive.Unity;

public class PlayerController : ReactiveMonoBehaviour
{
    protected override void InitializeReactive()
    {
        // These automatically run on Unity's main thread
        var health = UnityReactiveFlow.CreateState(100);
        var isAlive = UnityReactiveFlow.CreateComputed("isAlive", builder => 
            builder.Track(health) > 0);
        
        var deathEffect = UnityReactiveFlow.CreateMainThreadEffect("death", builder =>
        {
            if (!builder.Track(isAlive))
            {
                // Safe to call Unity APIs here
                gameObject.SetActive(false);
            }
        });
    }
}
```

### ReactiveMonoBehaviour

Base class for Unity components with reactive capabilities.

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;

public class HealthSystem : ReactiveMonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    
    private ReactiveState<int> health;
    private ReactiveState<bool> isInvulnerable;
    private ComputedValue<float> healthPercentage;
    private ComputedValue<bool> isAlive;
    
    protected override void InitializeReactive()
    {
        // Create reactive state
        health = CreateState(maxHealth);
        isInvulnerable = CreateState(false);
        
        // Create computed values
        healthPercentage = CreateComputed(builder =>
            (float)builder.Track(health) / maxHealth);
        
        isAlive = CreateComputed(builder => 
            builder.Track(health) > 0);
        
        // Create effects
        CreateEffect(builder =>
        {
            if (!builder.Track(isAlive))
            {
                Debug.Log("Player died!");
                // Trigger death sequence
            }
        });
        
        CreateEffect(builder =>
        {
            var percent = builder.Track(healthPercentage);
            if (percent < 0.2f && builder.Track(isAlive))
            {
                Debug.Log("Low health warning!");
            }
        });
    }
    
    public void TakeDamage(int damage)
    {
        if (!isInvulnerable.Current)
        {
            health.Update(current => Mathf.Max(0, current - damage));
        }
    }
    
    public void Heal(int amount)
    {
        health.Update(current => Mathf.Min(maxHealth, current + amount));
    }
    
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable.Set(invulnerable);
    }
    
    // Public read-only access
    public int CurrentHealth => health.Current;
    public float HealthPercentage => healthPercentage.Current;
    public bool IsAlive => isAlive.Current;
}
```

## Reactive UI Components

### ReactiveText

Automatically updates Unity Text components.

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;

public class ScoreDisplay : ReactiveMonoBehaviour
{
    [SerializeField] private ReactiveText scoreText;
    [SerializeField] private ReactiveText levelText;
    
    private ReactiveState<int> score;
    private ReactiveState<int> level;
    private ComputedValue<string> scoreDisplay;
    private ComputedValue<string> levelDisplay;
    
    protected override void InitializeReactive()
    {
        score = CreateState(0);
        level = CreateState(1);
        
        scoreDisplay = CreateComputed(builder =>
            $"Score: {builder.Track(score):N0}");
        
        levelDisplay = CreateComputed(builder =>
            $"Level {builder.Track(level)}");
        
        // Bind to UI
        CreateEffect(builder =>
        {
            scoreText.SetText(builder.Track(scoreDisplay));
        });
        
        CreateEffect(builder =>
        {
            levelText.SetText(builder.Track(levelDisplay));
        });
    }
    
    public void AddScore(int points) => score.Update(s => s + points);
    public void SetLevel(int newLevel) => level.Set(newLevel);
}
```

### ReactiveButton

Reactive button with state management.

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;

public class ShopButton : ReactiveMonoBehaviour
{
    [SerializeField] private ReactiveButton buyButton;
    [SerializeField] private ReactiveText priceText;
    [SerializeField] private int itemPrice = 100;
    
    private ReactiveState<int> playerMoney;
    private ComputedValue<bool> canAfford;
    private ComputedValue<string> buttonText;
    
    protected override void InitializeReactive()
    {
        playerMoney = CreateState(50);
        
        canAfford = CreateComputed(builder =>
            builder.Track(playerMoney) >= itemPrice);
        
        buttonText = CreateComputed(builder =>
        {
            var affordable = builder.Track(canAfford);
            return affordable ? $"Buy (${itemPrice})" : "Not enough money";
        });
        
        // Update button state
        CreateEffect(builder =>
        {
            buyButton.SetInteractable(builder.Track(canAfford));
        });
        
        CreateEffect(builder =>
        {
            priceText.SetText(builder.Track(buttonText));
        });
        
        // Handle button clicks
        CreateEffect(builder =>
        {
            var clickCount = builder.Track(buyButton.ClickCount);
            if (clickCount > 0 && canAfford.Current)
            {
                playerMoney.Update(money => money - itemPrice);
                Debug.Log("Item purchased!");
            }
        });
    }
    
    public void SetPlayerMoney(int money) => playerMoney.Set(money);
}
```

### ReactiveImage

Reactive image with color and alpha control.

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;

public class HealthBar : ReactiveMonoBehaviour
{
    [SerializeField] private ReactiveImage healthBarFill;
    [SerializeField] private ReactiveImage healthBarBackground;
    
    private ReactiveState<float> healthPercentage;
    private ComputedValue<Color> healthColor;
    private ComputedValue<float> fillAmount;
    
    protected override void InitializeReactive()
    {
        healthPercentage = CreateState(1.0f);
        
        healthColor = CreateComputed(builder =>
        {
            var percent = builder.Track(healthPercentage);
            return percent > 0.6f ? Color.green :
                   percent > 0.3f ? Color.yellow : Color.red;
        });
        
        fillAmount = CreateComputed(builder =>
            Mathf.Clamp01(builder.Track(healthPercentage)));
        
        // Update health bar appearance
        CreateEffect(builder =>
        {
            healthBarFill.Color.Set(builder.Track(healthColor));
        });
        
        CreateEffect(builder =>
        {
            var rect = healthBarFill.GetComponent<RectTransform>();
            var fill = builder.Track(fillAmount);
            rect.anchorMax = new Vector2(fill, 1f);
        });
    }
    
    public void SetHealthPercentage(float percentage)
    {
        healthPercentage.Set(Mathf.Clamp01(percentage));
    }
}
```

## Game State Management

### Game Manager with Reactive State

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;

public enum GameState { Menu, Playing, Paused, GameOver }

public class GameManager : ReactiveMonoBehaviour
{
    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject gameOverUI;
    
    private ReactiveState<GameState> gameState;
    private ReactiveState<float> gameTime;
    private ReactiveState<int> score;
    private ReactiveState<bool> isPaused;
    
    private ComputedValue<bool> isPlaying;
    private ComputedValue<bool> canPause;
    private ComputedValue<string> timeDisplay;
    
    protected override void InitializeReactive()
    {
        gameState = CreateState(GameState.Menu);
        gameTime = CreateState(0f);
        score = CreateState(0);
        isPaused = CreateState(false);
        
        isPlaying = CreateComputed(builder =>
            builder.Track(gameState) == GameState.Playing);
        
        canPause = CreateComputed(builder =>
            builder.Track(isPlaying) && !builder.Track(isPaused));
        
        timeDisplay = CreateComputed(builder =>
        {
            var time = builder.Track(gameTime);
            var minutes = Mathf.FloorToInt(time / 60);
            var seconds = Mathf.FloorToInt(time % 60);
            return $"{minutes:00}:{seconds:00}";
        });
        
        // UI state management
        CreateEffect(builder =>
        {
            var state = builder.Track(gameState);
            menuUI.SetActive(state == GameState.Menu);
            gameUI.SetActive(state == GameState.Playing);
            pauseUI.SetActive(state == GameState.Paused);
            gameOverUI.SetActive(state == GameState.GameOver);
        });
        
        // Time management
        CreateEffect(builder =>
        {
            var playing = builder.Track(isPlaying);
            var paused = builder.Track(isPaused);
            Time.timeScale = (playing && !paused) ? 1f : 0f;
        });
    }
    
    private void Update()
    {
        if (isPlaying.Current && !isPaused.Current)
        {
            gameTime.Update(time => time + Time.deltaTime);
        }
    }
    
    public void StartGame()
    {
        gameState.Set(GameState.Playing);
        gameTime.Set(0f);
        score.Set(0);
        isPaused.Set(false);
    }
    
    public void PauseGame()
    {
        if (canPause.Current)
        {
            gameState.Set(GameState.Paused);
        }
    }
    
    public void ResumeGame()
    {
        if (gameState.Current == GameState.Paused)
        {
            gameState.Set(GameState.Playing);
        }
    }
    
    public void EndGame()
    {
        gameState.Set(GameState.GameOver);
    }
    
    public void ReturnToMenu()
    {
        gameState.Set(GameState.Menu);
    }
    
    public void AddScore(int points) => score.Update(s => s + points);
    
    // Public read-only access
    public GameState CurrentState => gameState.Current;
    public float CurrentTime => gameTime.Current;
    public int CurrentScore => score.Current;
    public string TimeDisplay => timeDisplay.Current;
}
```

## Animation Integration

### Reactive Animations

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;
using DG.Tweening; // DOTween integration

public class ReactiveAnimator : ReactiveMonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float animationDuration = 0.5f;
    
    private ReactiveState<Vector3> targetPosition;
    private ReactiveState<float> targetScale;
    private ReactiveState<Color> targetColor;
    
    private Renderer targetRenderer;
    
    protected override void InitializeReactive()
    {
        targetRenderer = targetTransform.GetComponent<Renderer>();
        
        targetPosition = CreateState(targetTransform.position);
        targetScale = CreateState(1f);
        targetColor = CreateState(Color.white);
        
        // Animate position changes
        CreateEffect(builder =>
        {
            var position = builder.Track(targetPosition);
            targetTransform.DOMove(position, animationDuration);
        });
        
        // Animate scale changes
        CreateEffect(builder =>
        {
            var scale = builder.Track(targetScale);
            targetTransform.DOScale(scale, animationDuration);
        });
        
        // Animate color changes
        CreateEffect(builder =>
        {
            var color = builder.Track(targetColor);
            if (targetRenderer != null)
            {
                targetRenderer.material.DOColor(color, animationDuration);
            }
        });
    }
    
    public void MoveTo(Vector3 position) => targetPosition.Set(position);
    public void ScaleTo(float scale) => targetScale.Set(scale);
    public void ChangeColor(Color color) => targetColor.Set(color);
}
```

## Performance Optimization

### Batched Updates in Unity

```csharp
using Ludo.Reactive.Unity;
using UnityEngine;

public class EfficientUpdater : ReactiveMonoBehaviour
{
    private ReactiveState<Vector3> position;
    private ReactiveState<Quaternion> rotation;
    private ReactiveState<Vector3> scale;
    
    protected override void InitializeReactive()
    {
        position = CreateState(transform.position);
        rotation = CreateState(transform.rotation);
        scale = CreateState(transform.localScale);
        
        // Batch transform updates
        CreateEffect(builder =>
        {
            var pos = builder.Track(position);
            var rot = builder.Track(rotation);
            var scl = builder.Track(scale);
            
            // All transform changes happen together
            transform.SetPositionAndRotation(pos, rot);
            transform.localScale = scl;
        });
    }
    
    public void UpdateTransform(Vector3 newPos, Quaternion newRot, Vector3 newScale)
    {
        // Batch all updates to prevent multiple effect executions
        UnityReactiveFlow.Scheduler.ExecuteBatch(() =>
        {
            position.Set(newPos);
            rotation.Set(newRot);
            scale.Set(newScale);
        });
    }
}
```

## Testing in Unity

### Unit Testing Reactive Components

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ludo.Reactive.Unity;

public class HealthSystemTests
{
    private GameObject testObject;
    private HealthSystem healthSystem;
    
    [SetUp]
    public void Setup()
    {
        testObject = new GameObject("TestHealthSystem");
        healthSystem = testObject.AddComponent<HealthSystem>();
    }
    
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
    }
    
    [Test]
    public void TakeDamage_ReducesHealth()
    {
        // Arrange
        var initialHealth = healthSystem.CurrentHealth;
        
        // Act
        healthSystem.TakeDamage(20);
        
        // Assert
        Assert.AreEqual(initialHealth - 20, healthSystem.CurrentHealth);
    }
    
    [Test]
    public void HealthPercentage_UpdatesCorrectly()
    {
        // Arrange & Act
        healthSystem.TakeDamage(50);
        
        // Assert
        Assert.AreEqual(0.5f, healthSystem.HealthPercentage, 0.01f);
    }
    
    [UnityTest]
    public System.Collections.IEnumerator Death_TriggersCorrectly()
    {
        // Arrange
        var wasAlive = healthSystem.IsAlive;
        
        // Act
        healthSystem.TakeDamage(200); // More than max health
        yield return null; // Wait one frame for effects to process
        
        // Assert
        Assert.IsTrue(wasAlive);
        Assert.IsFalse(healthSystem.IsAlive);
    }
}
```

## Best Practices for Unity

1. **Always use ReactiveMonoBehaviour** for Unity components
2. **Use CreateState/CreateEffect/CreateComputed** methods for automatic cleanup
3. **Prefer UnityReactiveFlow** for Unity-specific reactive operations
4. **Batch updates** when changing multiple values simultaneously
5. **Use main thread effects** for Unity API calls
6. **Test reactive logic** with unit tests
7. **Profile performance** in complex reactive hierarchies
