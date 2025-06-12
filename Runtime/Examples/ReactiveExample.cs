using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ludo.Reactive.Examples
{
    /// <summary>
    /// Example script demonstrating basic usage of the Ludo.Reactive system.
    /// This script shows how to use ReactiveProperty, observable sequences, and Unity integration.
    /// </summary>
    public class ReactiveExample : MonoBehaviour
    {
        [Header("Reactive Properties")]
        [SerializeField] private ReactiveProperty<int> _health = new ReactiveProperty<int>(100);
        [SerializeField] private ReactiveProperty<float> _mana = new ReactiveProperty<float>(50f);
        [SerializeField] private ReactiveProperty<string> _playerName = new ReactiveProperty<string>("Player");

        [Header("UI References")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider manaSlider;
        [SerializeField] private Text healthText;
        [SerializeField] private Text manaText;
        [SerializeField] private Text playerNameText;
        [SerializeField] private Button damageButton;
        [SerializeField] private Button healButton;
        [SerializeField] private InputField nameInput;

        [Header("Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private int damageAmount = 10;
        [SerializeField] private int healAmount = 15;

        /// <summary>
        /// Read-only access to health for other scripts.
        /// </summary>
        public IReadOnlyReactiveProperty<int> Health => _health.AsReadOnly();

        /// <summary>
        /// Read-only access to mana for other scripts.
        /// </summary>
        public IReadOnlyReactiveProperty<float> Mana => _mana.AsReadOnly();

        /// <summary>
        /// Read-only access to player name for other scripts.
        /// </summary>
        public IReadOnlyReactiveProperty<string> PlayerName => _playerName.AsReadOnly();

        private void Start()
        {
            SetupHealthSystem();
            SetupManaSystem();
            SetupPlayerNameSystem();
            SetupUIBindings();
            SetupGameplayLogic();
        }

        private void SetupHealthSystem()
        {
            // Update health UI when health changes
            _health
                .Subscribe(health => 
                {
                    if (healthText != null)
                        healthText.text = $"Health: {health}/{maxHealth}";
                    
                    if (healthSlider != null)
                        healthSlider.value = (float)health / maxHealth;
                })
                .AddTo(this);

            // Health percentage for other systems
            var healthPercentage = _health
                .Select(health => (float)health / maxHealth)
                .DistinctUntilChanged();

            // Log when health gets low
            healthPercentage
                .Where(percentage => percentage <= 0.2f)
                .Subscribe(_ => Debug.Log("Health is critically low!"))
                .AddTo(this);

            // Death detection
            _health
                .Where(health => health <= 0)
                .Take(1)
                .Subscribe(_ => OnPlayerDeath())
                .AddTo(this);
        }

        private void SetupManaSystem()
        {
            // Update mana UI when mana changes
            _mana
                .Subscribe(mana => 
                {
                    if (manaText != null)
                        manaText.text = $"Mana: {mana:F1}/{maxMana}";
                    
                    if (manaSlider != null)
                        manaSlider.value = mana / maxMana;
                })
                .AddTo(this);

            // Mana regeneration over time
            Observable.Interval(TimeSpan.FromSeconds(1))
                .Where(_ => _mana.Value < maxMana)
                .Subscribe(_ => 
                {
                    _mana.Value = Mathf.Min(_mana.Value + 2f, maxMana);
                })
                .AddTo(this);
        }

        private void SetupPlayerNameSystem()
        {
            // Update player name UI when name changes
            _playerName
                .Subscribe(name => 
                {
                    if (playerNameText != null)
                        playerNameText.text = $"Player: {name}";
                })
                .AddTo(this);

            // Validate player name
            _playerName
                .Where(name => string.IsNullOrEmpty(name))
                .Subscribe(_ => 
                {
                    Debug.LogWarning("Player name cannot be empty!");
                    _playerName.Value = "Unknown Player";
                })
                .AddTo(this);
        }

        private void SetupUIBindings()
        {
            // Damage button
            if (damageButton != null)
            {
                damageButton.OnClickAsObservable()
                    .Where(_ => _health.Value > 0)
                    .Subscribe(_ => TakeDamage(damageAmount))
                    .AddTo(this);
            }

            // Heal button
            if (healButton != null)
            {
                healButton.OnClickAsObservable()
                    .Where(_ => _health.Value > 0 && _health.Value < maxHealth)
                    .Subscribe(_ => Heal(healAmount))
                    .AddTo(this);
            }

            // Name input
            if (nameInput != null)
            {
                nameInput.OnEndEditAsObservable()
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Subscribe(name => _playerName.Value = name.Trim())
                    .AddTo(this);
            }
        }

        private void SetupGameplayLogic()
        {
            // Combine health and mana for overall status
            var playerStatus = _health
                .CombineLatest(_mana, (health, mana) => new { Health = health, Mana = mana })
                .DistinctUntilChanged();

            playerStatus
                .Subscribe(status => 
                {
                    Debug.Log($"Player Status - Health: {status.Health}, Mana: {status.Mana:F1}");
                })
                .AddTo(this);

            // Example of using Do operator for side effects
            _health
                .Do(health => Debug.Log($"Health changed to: {health}"))
                .Where(health => health % 10 == 0) // Only log every 10 health points
                .Subscribe(health => Debug.Log($"Health milestone reached: {health}"))
                .AddTo(this);

            // Example of throttling rapid changes
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Space))
                .Throttle(TimeSpan.FromSeconds(0.5f)) // Prevent spam clicking
                .Subscribe(_ => TakeDamage(5))
                .AddTo(this);
        }

        public void TakeDamage(int damage)
        {
            if (_health.Value > 0)
            {
                _health.Value = Mathf.Max(0, _health.Value - damage);
                Debug.Log($"Player took {damage} damage!");
            }
        }

        public void Heal(int healAmount)
        {
            if (_health.Value > 0)
            {
                _health.Value = Mathf.Min(maxHealth, _health.Value + healAmount);
                Debug.Log($"Player healed for {healAmount} health!");
            }
        }

        public void UseMana(float amount)
        {
            if (_mana.Value >= amount)
            {
                _mana.Value -= amount;
                Debug.Log($"Used {amount} mana!");
            }
            else
            {
                Debug.Log("Not enough mana!");
            }
        }

        private void OnPlayerDeath()
        {
            Debug.Log("Player has died!");
            
            // Example of delayed respawn
            Observable.Timer(TimeSpan.FromSeconds(3))
                .Subscribe(_ => 
                {
                    _health.Value = maxHealth;
                    _mana.Value = maxMana;
                    Debug.Log("Player respawned!");
                })
                .AddTo(this);
        }

        private void OnDestroy()
        {
            // Cleanup is handled automatically by AddTo(this)
            // But we can also manually dispose if needed
            _health?.Dispose();
            _mana?.Dispose();
            _playerName?.Dispose();
        }

        // Example of exposing reactive properties for inspector debugging
        [ContextMenu("Debug Health")]
        private void DebugHealth()
        {
            Debug.Log($"Current Health: {_health.Value}, Observers: {_health.ObserverCount}");
        }

        [ContextMenu("Debug Mana")]
        private void DebugMana()
        {
            Debug.Log($"Current Mana: {_mana.Value}, Observers: {_mana.ObserverCount}");
        }
    }
}
