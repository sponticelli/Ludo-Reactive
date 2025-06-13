using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ludo.Reactive.WebGL;

namespace Ludo.Reactive.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating all new features in Ludo.Reactive v1.1.0.
    /// Shows MessageBroker, TestScheduler, ObjectPool, WebGL optimizations, and enhanced UI integrations.
    /// </summary>
    public class ReactiveV11Example : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Toggle godModeToggle;
        [SerializeField] private Dropdown difficultyDropdown;
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private Button damageButton;
        [SerializeField] private Button healButton;

        [Header("Reactive Properties")]
        [SerializeField] private ReactiveProperty<float> playerHealth = new ReactiveProperty<float>(100f);
        [SerializeField] private ReactiveProperty<bool> godModeEnabled = new ReactiveProperty<bool>(false);
        [SerializeField] private ReactiveProperty<GameDifficulty> currentDifficulty = new ReactiveProperty<GameDifficulty>(GameDifficulty.Normal);

        private CompositeDisposable _disposables;

        private void Start()
        {
            _disposables = new CompositeDisposable();
            
            SetupMessageBrokerExample();
            SetupUIBindingExample();
            SetupWebGLOptimizationExample();
            SetupObjectPoolingExample();
        }

        private void SetupMessageBrokerExample()
        {
            // Subscribe to global damage events
            _disposables.Add(this.SubscribeToMessage<DamageEvent>(OnDamageReceived));

            // Subscribe to filtered healing events (only when health < 50)
            _disposables.Add(this.SubscribeToMessage<HealEvent>(
                heal => playerHealth.Value < 50f,
                OnHealReceived
            ));

            // Subscribe to difficulty change events
            _disposables.Add(MessageBroker.Global.Subscribe<DifficultyChangedEvent>(OnDifficultyChanged));

            // Setup button events to publish messages
            if (damageButton != null)
            {
                _disposables.Add(damageButton.OnClickAsObservable()
                    .Subscribe(_ => new DamageEvent { Amount = 20f, Source = "Button" }.Publish()));
            }

            if (healButton != null)
            {
                _disposables.Add(healButton.OnClickAsObservable()
                    .Subscribe(_ => new HealEvent { Amount = 15f, Source = "Button" }.Publish()));
            }
        }

        private void SetupUIBindingExample()
        {
            // Bind health slider with two-way reactive binding
            if (healthSlider != null)
            {
                _disposables.Add(healthSlider.BindToReactiveProperty(playerHealth));
            }

            // Bind god mode toggle
            if (godModeToggle != null)
            {
                _disposables.Add(godModeToggle.BindToReactiveProperty(godModeEnabled));
            }

            // Bind difficulty dropdown with enum support
            if (difficultyDropdown != null)
            {
                _disposables.Add(difficultyDropdown.BindToReactiveProperty(currentDifficulty));

                // Also subscribe to selection changes for rich data
                _disposables.Add(difficultyDropdown.OnSelectionChangedAsObservable()
                    .Subscribe(data => Debug.Log($"Difficulty changed to: {data.Text} (Index: {data.Index})")));
            }

            // Setup scroll rect monitoring
            if (logScrollRect != null)
            {
                _disposables.Add(logScrollRect.OnScrollAsObservable()
                    .Subscribe(data =>
                    {
                        if (data.IsAtBottom)
                        {
                            Debug.Log("Scrolled to bottom of log");
                        }
                    }));

                // Monitor for near-end scrolling (infinite scroll simulation)
                _disposables.Add(logScrollRect.OnNearEndAsObservable(0.1f)
                    .Subscribe(_ => Debug.Log("Near end of scroll - could load more content")));
            }
        }

        private void SetupWebGLOptimizationExample()
        {
            // Create WebGL-optimized interval for periodic health regeneration
            var regenInterval = WebGLOptimizations.IntervalOptimized(TimeSpan.FromSeconds(2f));

            _disposables.Add(regenInterval
                .Where(_ => !godModeEnabled.Value && playerHealth.Value < 100f)
                .Subscribe(_ =>
                {
                    playerHealth.Value = Mathf.Min(100f, playerHealth.Value + 1f);
                    Debug.Log($"Health regenerated: {playerHealth.Value}");
                }));

            // Create batched damage events for performance
            var damageStream = MessageBroker.Global.GetObservable<DamageEvent>();

            _disposables.Add(damageStream
                .BatchForWebGL(3, TimeSpan.FromSeconds(1f)) // Batch up to 3 damage events or 1 second
                .Subscribe(damageBatch =>
                {
                    var totalDamage = 0f;
                    foreach (var damage in damageBatch)
                    {
                        totalDamage += damage.Amount;
                    }
                    Debug.Log($"Processed batch of {damageBatch.Count} damage events, total: {totalDamage}");
                }));

            // Use cached observable for frequently accessed data
            var optimizedHealthStream = WebGLOptimizations.CreateCached<float>(() =>
                playerHealth.OptimizeForWebGL()
            );

            _disposables.Add(optimizedHealthStream
                .Where(health => health <= 0f)
                .Subscribe(_ => Debug.Log("Player died!")));
        }

        private void SetupObjectPoolingExample()
        {
            // Example of using pooled disposables for temporary subscriptions
            _disposables.Add(playerHealth
                .Where(health => health < 25f)
                .Subscribe(_ =>
                {
                    // Create a temporary subscription using pooled disposable
                    var tempSubscription = PooledDisposable.Create(() =>
                        Debug.Log("Low health warning disposed"));

                    // Auto-dispose after 3 seconds
                    Observable.Timer(TimeSpan.FromSeconds(3f))
                        .Subscribe(_ => tempSubscription.Dispose());
                }));

            // Example of using reactive object pools
            _disposables.Add(Observable.Interval(TimeSpan.FromSeconds(5f))
                .Subscribe(_ =>
                {
                    // Get a pooled list for temporary operations
                    using (ReactiveObjectPoolsExtensions.GetWithAutoReturn(out List<object> tempList))
                    {
                        tempList.Add($"Health check at {Time.time}");
                        tempList.Add($"Current health: {playerHealth.Value}");

                        Debug.Log($"Temporary health log created with {tempList.Count} entries");
                        // List automatically returned to pool when disposed
                    }
                }));
        }

        private void OnDamageReceived(DamageEvent damageEvent)
        {
            if (!godModeEnabled.Value)
            {
                playerHealth.Value = Mathf.Max(0f, playerHealth.Value - damageEvent.Amount);
                Debug.Log($"Received {damageEvent.Amount} damage from {damageEvent.Source}. Health: {playerHealth.Value}");
            }
            else
            {
                Debug.Log($"God mode enabled - ignored {damageEvent.Amount} damage from {damageEvent.Source}");
            }
        }

        private void OnHealReceived(HealEvent healEvent)
        {
            playerHealth.Value = Mathf.Min(100f, playerHealth.Value + healEvent.Amount);
            Debug.Log($"Healed {healEvent.Amount} from {healEvent.Source}. Health: {playerHealth.Value}");
        }

        private void OnDifficultyChanged(DifficultyChangedEvent difficultyEvent)
        {
            Debug.Log($"Game difficulty changed to: {difficultyEvent.NewDifficulty}");
            
            // Adjust damage multiplier based on difficulty
            var multiplier = difficultyEvent.NewDifficulty switch
            {
                GameDifficulty.Easy => 0.5f,
                GameDifficulty.Normal => 1.0f,
                GameDifficulty.Hard => 1.5f,
                GameDifficulty.Nightmare => 2.0f,
                _ => 1.0f
            };
            
            Debug.Log($"Damage multiplier set to: {multiplier}");
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
            playerHealth?.Dispose();
            godModeEnabled?.Dispose();
            currentDifficulty?.Dispose();
        }

        // Example of using TestScheduler for deterministic testing
        [ContextMenu("Run Test Scheduler Example")]
        private void RunTestSchedulerExample()
        {
            var testScheduler = UnitySchedulers.CreateTestScheduler();
            var executionLog = new List<string>();

            // Schedule some test actions
            testScheduler.Schedule(TimeSpan.FromSeconds(1), () => 
                executionLog.Add("Action 1 executed"));
            
            testScheduler.Schedule(TimeSpan.FromSeconds(2), () => 
                executionLog.Add("Action 2 executed"));
            
            testScheduler.Schedule(TimeSpan.FromSeconds(3), () => 
                executionLog.Add("Action 3 executed"));

            // Advance time manually for testing
            testScheduler.AdvanceBy(TimeSpan.FromSeconds(1.5));
            Debug.Log($"After 1.5s: {string.Join(", ", executionLog)}");

            testScheduler.AdvanceBy(TimeSpan.FromSeconds(2));
            Debug.Log($"After 3.5s total: {string.Join(", ", executionLog)}");

            testScheduler.Dispose();
        }
    }

    // Message event classes for MessageBroker examples
    public class DamageEvent
    {
        public float Amount { get; set; }
        public string Source { get; set; }
    }

    public class HealEvent
    {
        public float Amount { get; set; }
        public string Source { get; set; }
    }

    public class DifficultyChangedEvent
    {
        public GameDifficulty NewDifficulty { get; set; }
        public GameDifficulty OldDifficulty { get; set; }
    }

    public enum GameDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Nightmare = 3
    }
}
