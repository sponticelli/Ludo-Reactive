# Ludo.Reactive - Examples

## Table of Contents
1. [Basic Examples](#basic-examples)
2. [Game Development Patterns](#game-development-patterns)
3. [UI Reactive Bindings](#ui-reactive-bindings)
4. [Input Handling](#input-handling)
5. [Animation & Effects](#animation--effects)
6. [Networking](#networking)
7. [Performance Optimization](#performance-optimization)
8. [Testing Patterns](#testing-patterns)

## Basic Examples

### Simple Health System

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
            .AddTo(this);
        
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
    
    private void OnPlayerDeath()
    {
        Debug.Log("Player died!");
        // Handle death logic
    }
}
```

### Observable Timer

```csharp
public class GameTimer : MonoBehaviour
{
    [SerializeField] private Text timerText;
    
    void Start()
    {
        // Count down from 60 seconds
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Take(60)
            .Select(tick => 60 - tick - 1)
            .Subscribe(timeLeft => 
            {
                timerText.text = $"Time: {timeLeft}";
                if (timeLeft == 0)
                    OnTimeUp();
            })
            .AddTo(this);
    }
    
    private void OnTimeUp()
    {
        Debug.Log("Time's up!");
    }
}
```

### Button Click Handling

```csharp
public class MenuController : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    void Start()
    {
        // Play button with spam protection
        playButton.OnClickAsObservable()
            .Throttle(TimeSpan.FromSeconds(0.5f))
            .Subscribe(_ => StartGame())
            .AddTo(this);
        
        // Settings button
        settingsButton.OnClickAsObservable()
            .Subscribe(_ => OpenSettings())
            .AddTo(this);
        
        // Quit button with confirmation
        quitButton.OnClickAsObservable()
            .Subscribe(_ => ConfirmQuit())
            .AddTo(this);
    }
}
```

## Game Development Patterns

### Inventory System

```csharp
public class InventorySystem : MonoBehaviour
{
    private ReactiveCollection<Item> _items = new ReactiveCollection<Item>();
    private ReactiveProperty<int> _gold = new ReactiveProperty<int>(0);
    
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Text goldText;
    
    public IReadOnlyReactiveProperty<int> Gold => _gold.AsReadOnly();
    
    void Start()
    {
        // Auto-update gold display
        _gold
            .Subscribe(gold => goldText.text = $"Gold: {gold:N0}")
            .AddTo(this);
        
        // Handle item additions
        _items.ObserveAdd()
            .Subscribe(addEvent => CreateItemUI(addEvent.Value))
            .AddTo(this);
            
        // Handle item removals
        _items.ObserveRemove()
            .Subscribe(removeEvent => RemoveItemUI(removeEvent.Value))
            .AddTo(this);
        
        // Highlight new items briefly
        _items.ObserveAdd()
            .Select(x => x.Value)
            .Delay(TimeSpan.FromSeconds(0.1f))
            .Subscribe(item => HighlightNewItem(item))
            .AddTo(this);
    }
    
    public void AddItem(Item item)
    {
        _items.Add(item);
    }
    
    public void RemoveItem(Item item)
    {
        _items.Remove(item);
    }
    
    public void AddGold(int amount)
    {
        _gold.Value += amount;
    }
}
```

### Combo System

```csharp
public class ComboSystem : MonoBehaviour
{
    [SerializeField] private float comboWindowSeconds = 0.5f;
    [SerializeField] private KeyCode[] comboSequence = { KeyCode.W, KeyCode.W, KeyCode.S, KeyCode.S, KeyCode.A };
    
    private Subject<KeyCode> _inputStream = new Subject<KeyCode>();
    
    void Start()
    {
        // Detect specific combo sequence
        _inputStream
            .Buffer(comboSequence.Length, 1)
            .Where(buffer => buffer.SequenceEqual(comboSequence))
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
        // Capture input
        if (Input.anyKeyDown)
        {
            foreach (KeyCode key in comboSequence)
            {
                if (Input.GetKeyDown(key))
                {
                    _inputStream.OnNext(key);
                    break;
                }
            }
        }
    }
    
    private void ExecuteSpecialMove()
    {
        Debug.Log("Special move executed!");
    }
    
    private void ResetComboMultiplier()
    {
        Debug.Log("Combo reset");
    }
}
```

### Synchronized Animation System

```csharp
public class SynchronizedAnimationSystem : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private ParticleSystem effectSystem;

    private Subject<string> _animationTriggers = new Subject<string>();
    private Subject<AudioClip> _audioTriggers = new Subject<AudioClip>();

    void Start()
    {
        // Synchronize animations with audio using Zip
        // Both streams must emit for the synchronized effect to play
        _animationTriggers
            .Zip(_audioTriggers, (animTrigger, audioClip) => new { Animation = animTrigger, Audio = audioClip })
            .Subscribe(sync =>
            {
                // Play animation and audio simultaneously
                characterAnimator.SetTrigger(sync.Animation);
                audioSource.PlayOneShot(sync.Audio);

                // Add visual effect
                effectSystem.Play();

                Debug.Log($"Synchronized: {sync.Animation} with {sync.Audio.name}");
            })
            .AddTo(this);

        // Example: Trigger attack animation and sound together
        Observable.Interval(TimeSpan.FromSeconds(3))
            .Take(5)
            .Subscribe(tick =>
            {
                // These will be paired by index (0 with 0, 1 with 1, etc.)
                _animationTriggers.OnNext($"Attack_{tick}");
                _audioTriggers.OnNext(GetAttackSound((int)tick));
            })
            .AddTo(this);
    }

    public void TriggerSynchronizedAction(string animationTrigger, AudioClip audioClip)
    {
        _animationTriggers.OnNext(animationTrigger);
        _audioTriggers.OnNext(audioClip);
    }

    private AudioClip GetAttackSound(int index)
    {
        // Return appropriate audio clip for the attack
        return Resources.Load<AudioClip>($"Audio/Attack_{index}");
    }
}
```

### State Machine

```csharp
public enum GameState { Menu, Playing, Paused, GameOver }

public class GameStateManager : MonoBehaviour
{
    private ReactiveProperty<GameState> _currentState = new ReactiveProperty<GameState>(GameState.Menu);
    
    public IReadOnlyReactiveProperty<GameState> CurrentState => _currentState.AsReadOnly();
    
    void Start()
    {
        // React to state changes
        _currentState
            .DistinctUntilChanged()
            .Subscribe(state => OnStateChanged(state))
            .AddTo(this);
        
        // Auto-pause when losing focus
        this.OnApplicationFocusAsObservable()
            .Where(focused => !focused && _currentState.Value == GameState.Playing)
            .Subscribe(_ => SetState(GameState.Paused))
            .AddTo(this);
    }
    
    public void SetState(GameState newState)
    {
        _currentState.Value = newState;
    }
    
    private void OnStateChanged(GameState state)
    {
        Debug.Log($"Game state changed to: {state}");
        
        switch (state)
        {
            case GameState.Menu:
                Time.timeScale = 1f;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
        }
    }
}
```

## UI Reactive Bindings

### Form Validation

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
            .Select(text => text.Length >= 3)
            .DistinctUntilChanged();
            
        var passwordValid = passwordInput.OnValueChangedAsObservable()
            .Select(text => text.Length >= 8)
            .DistinctUntilChanged();
        
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
        
        passwordInput.OnValueChangedAsObservable()
            .Where(text => text.Length > 0 && text.Length < 8)
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(_ => ShowError("Password must be at least 8 characters"))
            .AddTo(this);
        
        // Clear errors when fields become valid
        usernameValid.CombineLatest(passwordValid, (u, p) => u && p)
            .Where(valid => valid)
            .Subscribe(_ => ClearError())
            .AddTo(this);
    }
    
    private void ShowError(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
    }
    
    private void ClearError()
    {
        errorText.text = "";
    }
}
```

### Dynamic UI Updates

```csharp
public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private Text levelText;
    [SerializeField] private Text experienceText;
    [SerializeField] private Button levelUpButton;
    
    private PlayerStats playerStats;
    
    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        
        // Health bar
        playerStats.Health
            .CombineLatest(playerStats.MaxHealth, (current, max) => (float)current / max)
            .Subscribe(ratio => healthSlider.value = ratio)
            .AddTo(this);
        
        // Mana bar
        playerStats.Mana
            .CombineLatest(playerStats.MaxMana, (current, max) => (float)current / max)
            .Subscribe(ratio => manaSlider.value = ratio)
            .AddTo(this);
        
        // Level display
        playerStats.Level
            .Subscribe(level => levelText.text = $"Level {level}")
            .AddTo(this);
        
        // Experience display
        playerStats.Experience
            .CombineLatest(playerStats.ExperienceToNextLevel, (current, needed) => 
                $"XP: {current}/{needed}")
            .Subscribe(text => experienceText.text = text)
            .AddTo(this);
        
        // Level up button availability
        playerStats.Experience
            .CombineLatest(playerStats.ExperienceToNextLevel, (current, needed) => current >= needed)
            .Subscribe(canLevelUp => levelUpButton.interactable = canLevelUp)
            .AddTo(this);
    }
}
```

## Input Handling

### Movement Input

```csharp
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Continuous movement input
        this.UpdateAsObservable()
            .Select(_ => new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")))
            .Where(input => input.magnitude > 0.1f)
            .Subscribe(input => Move(input))
            .AddTo(this);
        
        // Jump input (discrete)
        this.UpdateAsObservable()
            .Where(_ => Input.GetKeyDown(KeyCode.Space))
            .Subscribe(_ => Jump())
            .AddTo(this);
    }
    
    private void Move(Vector2 input)
    {
        rb.velocity = input.normalized * moveSpeed;
    }
    
    private void Jump()
    {
        rb.AddForce(Vector2.up * 10f, ForceMode2D.Impulse);
    }
}
```

### Input Buffering

```csharp
public class BufferedInput : MonoBehaviour
{
    [SerializeField] private float bufferTime = 0.2f;
    
    private Subject<string> _inputBuffer = new Subject<string>();
    
    void Start()
    {
        // Buffer jump inputs
        this.UpdateAsObservable()
            .Where(_ => Input.GetKeyDown(KeyCode.Space))
            .Subscribe(_ => _inputBuffer.OnNext("Jump"))
            .AddTo(this);
        
        // Consume buffered inputs when grounded
        this.UpdateAsObservable()
            .Where(_ => IsGrounded())
            .SelectMany(_ => _inputBuffer.Take(1).Timeout(TimeSpan.FromSeconds(bufferTime)))
            .Catch(Observable.Empty<string>()) // Ignore timeout
            .Subscribe(action => ExecuteAction(action))
            .AddTo(this);
    }
    
    private bool IsGrounded()
    {
        // Ground check logic
        return Physics2D.Raycast(transform.position, Vector2.down, 1.1f);
    }
    
    private void ExecuteAction(string action)
    {
        if (action == "Jump")
        {
            GetComponent<Rigidbody2D>().AddForce(Vector2.up * 10f, ForceMode2D.Impulse);
        }
    }
}
```

## Animation & Effects

### Damage Numbers

```csharp
public class DamageNumberSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageNumberPrefab;
    
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
                var isCritical = damages.Any(d => d.IsCritical);
                SpawnDamageNumber(total, position, isCritical);
            })
            .AddTo(this);
        
        // Critical hit effects
        _damageStream
            .Where(damage => damage.IsCritical)
            .Subscribe(damage => PlayCriticalHitEffect(damage.WorldPosition))
            .AddTo(this);
    }
    
    public void ReportDamage(float amount, Vector3 position, bool isCritical = false)
    {
        _damageStream.OnNext(new DamageEvent 
        { 
            Amount = amount, 
            WorldPosition = position, 
            IsCritical = isCritical 
        });
    }
    
    private void SpawnDamageNumber(float amount, Vector3 position, bool isCritical)
    {
        var damageNumber = Instantiate(damageNumberPrefab, position, Quaternion.identity);
        var text = damageNumber.GetComponent<Text>();
        text.text = amount.ToString("F0");
        text.color = isCritical ? Color.red : Color.white;
        
        // Animate and destroy
        Observable.Timer(TimeSpan.FromSeconds(1))
            .Subscribe(_ => Destroy(damageNumber))
            .AddTo(this);
    }
    
    private void PlayCriticalHitEffect(Vector3 position)
    {
        // Play particle effect, screen shake, etc.
        Debug.Log($"Critical hit at {position}!");
    }
}

[System.Serializable]
public struct DamageEvent
{
    public float Amount;
    public Vector3 WorldPosition;
    public bool IsCritical;
}
```

### Smooth Camera Follow

```csharp
public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    
    void Start()
    {
        if (target == null) return;
        
        // Smooth camera following
        this.UpdateAsObservable()
            .Select(_ => target.position + offset)
            .Subscribe(targetPosition =>
            {
                var smoothPosition = Vector3.Lerp(transform.position, targetPosition, 
                    Time.deltaTime / smoothTime);
                transform.position = smoothPosition;
            })
            .AddTo(this);
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
```

## Networking

### Network Message Handling

```csharp
public class NetworkManager : MonoBehaviour
{
    private Subject<NetworkMessage> _messageStream = new Subject<NetworkMessage>();
    
    void Start()
    {
        // Player movement messages with throttling
        _messageStream
            .OfType<PlayerMoveMessage>()
            .Where(msg => msg.PlayerId != GetLocalPlayerId())
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(msg => UpdatePlayerPosition(msg.PlayerId, msg.Position))
            .AddTo(this);
        
        // Chat messages with spam protection
        _messageStream
            .OfType<ChatMessage>()
            .GroupBy(msg => msg.SenderId)
            .SelectMany(group => group.Throttle(TimeSpan.FromSeconds(1)))
            .Subscribe(msg => DisplayChatMessage(msg))
            .AddTo(this);
        
        // Connection status monitoring
        Observable.Interval(TimeSpan.FromSeconds(5))
            .SelectMany(_ => CheckConnectionStatus())
            .DistinctUntilChanged()
            .Subscribe(connected => UpdateConnectionUI(connected))
            .AddTo(this);
    }
    
    public void SendMessage(NetworkMessage message)
    {
        // Send over network
        _messageStream.OnNext(message);
    }
    
    private IObservable<bool> CheckConnectionStatus()
    {
        return Observable.Return(true); // Simplified
    }
    
    private void UpdatePlayerPosition(int playerId, Vector3 position)
    {
        // Update player position
    }
    
    private void DisplayChatMessage(ChatMessage message)
    {
        Debug.Log($"{message.SenderName}: {message.Text}");
    }
    
    private void UpdateConnectionUI(bool connected)
    {
        Debug.Log($"Connection status: {(connected ? "Connected" : "Disconnected")}");
    }
    
    private int GetLocalPlayerId() => 0; // Simplified
}

public abstract class NetworkMessage { }
public class PlayerMoveMessage : NetworkMessage 
{ 
    public int PlayerId; 
    public Vector3 Position; 
}
public class ChatMessage : NetworkMessage 
{ 
    public int SenderId; 
    public string SenderName; 
    public string Text; 
}
```

## Performance Optimization

### Object Pooling with Observables

```csharp
public class BulletPool : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 100;
    
    private Queue<GameObject> _pool = new Queue<GameObject>();
    private Subject<GameObject> _returnStream = new Subject<GameObject>();
    
    void Start()
    {
        // Pre-warm pool
        Observable.Range(0, poolSize)
            .Subscribe(_ => 
            {
                var bullet = Instantiate(bulletPrefab);
                bullet.SetActive(false);
                _pool.Enqueue(bullet);
            })
            .AddTo(this);
        
        // Auto-return bullets after delay
        _returnStream
            .Delay(TimeSpan.FromSeconds(5))
            .Where(bullet => bullet != null && bullet.activeSelf)
            .Subscribe(bullet => ReturnToPool(bullet))
            .AddTo(this);
    }
    
    public GameObject GetBullet()
    {
        GameObject bullet;
        if (_pool.Count > 0)
        {
            bullet = _pool.Dequeue();
        }
        else
        {
            bullet = Instantiate(bulletPrefab);
        }
        
        bullet.SetActive(true);
        _returnStream.OnNext(bullet);
        return bullet;
    }
    
    public void ReturnToPool(GameObject bullet)
    {
        bullet.SetActive(false);
        _pool.Enqueue(bullet);
    }
}
```

### Performance Monitoring

```csharp
public class PerformanceMonitor : MonoBehaviour
{
    private Subject<float> _frameTimeStream = new Subject<float>();
    
    void Start()
    {
        // Calculate FPS
        var fps = _frameTimeStream
            .Buffer(TimeSpan.FromSeconds(1))
            .Select(times => times.Count)
            .DistinctUntilChanged();
        
        // Detect performance issues
        fps.Where(f => f < 30)
            .Throttle(TimeSpan.FromSeconds(5))
            .Subscribe(_ => 
            {
                Debug.LogWarning("Low FPS detected, reducing quality");
                QualitySettings.DecreaseLevel();
            })
            .AddTo(this);
        
        // Log performance spikes
        _frameTimeStream
            .Where(frameTime => frameTime > 50) // 50ms = 20fps
            .Buffer(TimeSpan.FromSeconds(1))
            .Where(spikes => spikes.Count > 5)
            .Subscribe(spikes => 
            {
                Debug.LogWarning($"Performance spikes detected: {spikes.Count} frames over 50ms");
            })
            .AddTo(this);
    }
    
    void Update()
    {
        _frameTimeStream.OnNext(Time.deltaTime * 1000f);
    }
}
```

## Testing Patterns

### Unit Testing with Observables

```csharp
[Test]
public void TestHealthSystem()
{
    var health = new ReactiveProperty<int>(100);
    var deathTriggered = false;
    
    // Subscribe to death event
    health
        .Where(h => h <= 0)
        .Take(1)
        .Subscribe(_ => deathTriggered = true);
    
    // Test damage
    health.Value = 50;
    Assert.IsFalse(deathTriggered);
    
    health.Value = 0;
    Assert.IsTrue(deathTriggered);
}

[Test]
public void TestComboSystem()
{
    var inputStream = new Subject<KeyCode>();
    var comboTriggered = false;
    var targetCombo = new[] { KeyCode.A, KeyCode.B, KeyCode.C };
    
    inputStream
        .Buffer(3, 1)
        .Where(buffer => buffer.SequenceEqual(targetCombo))
        .Subscribe(_ => comboTriggered = true);
    
    // Test correct sequence
    inputStream.OnNext(KeyCode.A);
    inputStream.OnNext(KeyCode.B);
    inputStream.OnNext(KeyCode.C);
    
    Assert.IsTrue(comboTriggered);
}
```

### Integration Testing

```csharp
[UnityTest]
public IEnumerator TestAsyncOperation()
{
    var completed = false;
    var progress = 0f;
    
    // Test async operation with progress
    Observable.Create<float>(observer =>
    {
        var routine = StartCoroutine(SimulateAsyncOperation(observer));
        return Disposable.Create(() => StopCoroutine(routine));
    })
    .Subscribe(
        p => progress = p,
        () => completed = true
    );
    
    // Wait for completion
    yield return new WaitUntil(() => completed);
    
    Assert.IsTrue(completed);
    Assert.AreEqual(1f, progress);
}

private IEnumerator SimulateAsyncOperation(IObserver<float> observer)
{
    for (int i = 0; i <= 10; i++)
    {
        observer.OnNext(i / 10f);
        yield return new WaitForSeconds(0.1f);
    }
    observer.OnCompleted();
}
```

---

These examples demonstrate practical usage patterns for Ludo.Reactive in Unity game development. For more detailed API information, see the [API Reference](API%20Reference.md).
