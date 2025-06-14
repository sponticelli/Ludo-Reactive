using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ludo.Reactive.StateManagement;
using Ludo.Reactive.Collections;

namespace Ludo.Reactive.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating all new features in Ludo.Reactive v1.2.0.
    /// Shows state management, reactive collections, undo/redo, and persistence.
    /// </summary>
    public class ReactiveV12Example : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button addItemButton;
        [SerializeField] private Button removeItemButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;
        [SerializeField] private Button saveStateButton;
        [SerializeField] private Button loadStateButton;
        [SerializeField] private Text stateDisplayText;
        [SerializeField] private Text collectionDisplayText;
        [SerializeField] private Text historyDisplayText;

        // State Management
        private ReactiveStore<GameState> _store;
        private CommandHistory<GameState> _commandHistory;
        private IStatePersistence<GameState> _persistence;

        // Reactive Collections
        private ObservableList<string> _playerInventory;
        private ObservableDictionary<string, int> _playerStats;
        private ObservableSet<string> _unlockedAchievements;
        private CollectionSynchronizer<string> _inventorySynchronizer;

        // Disposables
        private CompositeDisposable _disposables;

        private void Start()
        {
            _disposables = new CompositeDisposable();
            
            SetupStateManagement();
            SetupReactiveCollections();
            SetupUI();
            SetupPersistence();
        }

        private void SetupStateManagement()
        {
            // Initialize state
            var initialState = new GameState
            {
                PlayerName = "Player",
                Level = 1,
                Experience = 0,
                Gold = 100
            };

            // Create reducer
            var reducer = new GameStateReducer();

            // Create store with logging middleware
            _store = new ReactiveStore<GameState>(
                initialState,
                reducer,
                new LoggingMiddleware<GameState>(logState: true)
            );

            // Create command history for undo/redo
            _commandHistory = new CommandHistory<GameState>(maxHistorySize: 50);

            // Subscribe to state changes
            _disposables.Add(_store.StateChanged.Subscribe(OnStateChanged));
            _disposables.Add(_commandHistory.HistoryChanged.Subscribe(OnHistoryChanged));

            // Create selectors for specific parts of state
            var levelSelector = _store.Select(state => state.Level, "LevelSelector");
            var goldSelector = _store.Select(state => state.Gold, "GoldSelector");

            _disposables.Add(levelSelector.Subscribe(level => 
                Debug.Log($"Level changed to: {level}")));
            _disposables.Add(goldSelector.Subscribe(gold => 
                Debug.Log($"Gold changed to: {gold}")));
        }

        private void SetupReactiveCollections()
        {
            // Create observable collections
            _playerInventory = new ObservableList<string>();
            _playerStats = new ObservableDictionary<string, int>();
            _unlockedAchievements = new ObservableSet<string>();

            // Initialize with some data
            _playerInventory.AddRange(new[] { "Sword", "Shield", "Potion" });
            _playerStats["Strength"] = 10;
            _playerStats["Agility"] = 8;
            _playerStats["Intelligence"] = 12;
            _unlockedAchievements.Add("First Steps");

            // Subscribe to collection changes
            _disposables.Add(_playerInventory.Subscribe(changeSet =>
            {
                Debug.Log($"Inventory changed: {changeSet.GetSummary()}");
                UpdateCollectionDisplay();
            }));

            _disposables.Add(_playerStats.ObserveChanges().Subscribe(change =>
            {
                Debug.Log($"Stats changed: {change}");
            }));

            _disposables.Add(_unlockedAchievements.ObserveChanges().Subscribe(change =>
            {
                Debug.Log($"Achievements changed: {change}");
                
                // Check for level up achievement
                if (change.Type == CollectionChangeType.Add && 
                    _unlockedAchievements.Count >= 5)
                {
                    var command = new LevelUpCommand();
                    ExecuteCommand(command);
                }
            }));

            // Setup collection synchronization
            _inventorySynchronizer = new CollectionSynchronizer<string>(
                ConflictResolutionStrategy.SourceWins);
            
            var secondaryInventory = new ObservableList<string>();
            _disposables.Add(_inventorySynchronizer.AddCollection(_playerInventory, "Primary"));
            _disposables.Add(_inventorySynchronizer.AddCollection(secondaryInventory, "Secondary"));
        }

        private void SetupUI()
        {
            if (addItemButton != null)
            {
                _disposables.Add(addItemButton.OnClickAsObservable()
                    .Subscribe(_ => AddRandomItem()));
            }

            if (removeItemButton != null)
            {
                _disposables.Add(removeItemButton.OnClickAsObservable()
                    .Subscribe(_ => RemoveRandomItem()));
            }

            if (undoButton != null)
            {
                _disposables.Add(undoButton.OnClickAsObservable()
                    .Subscribe(_ => UndoLastCommand()));
            }

            if (redoButton != null)
            {
                _disposables.Add(redoButton.OnClickAsObservable()
                    .Subscribe(_ => RedoLastCommand()));
            }

            if (saveStateButton != null)
            {
                _disposables.Add(saveStateButton.OnClickAsObservable()
                    .Subscribe(_ => SaveGameState()));
            }

            if (loadStateButton != null)
            {
                _disposables.Add(loadStateButton.OnClickAsObservable()
                    .Subscribe(_ => LoadGameState()));
            }

            UpdateAllDisplays();
        }

        private void SetupPersistence()
        {
            // Use file-based persistence for this example
            _persistence = new FileStatePersistence<GameState>(
                defaultState: new GameState { PlayerName = "New Player", Level = 1, Experience = 0, Gold = 50 });
        }

        private void AddRandomItem()
        {
            var items = new[] { "Potion", "Scroll", "Gem", "Key", "Map", "Coin" };
            var randomItem = items[UnityEngine.Random.Range(0, items.Length)];
            
            var command = new AddItemCommand(randomItem);
            ExecuteCommand(command);
        }

        private void RemoveRandomItem()
        {
            if (_playerInventory.Count > 0)
            {
                var randomIndex = UnityEngine.Random.Range(0, _playerInventory.Count);
                var item = _playerInventory[randomIndex];
                
                var command = new RemoveItemCommand(item, randomIndex);
                ExecuteCommand(command);
            }
        }

        private void ExecuteCommand(IReversibleCommand<GameState> command)
        {
            var newState = _commandHistory.ExecuteCommand(command, _store.CurrentState);
            
            // Dispatch action to update store
            var action = new StateUpdateAction(newState);
            _store.Dispatch(action);
        }

        private void UndoLastCommand()
        {
            if (_commandHistory.CanUndo)
            {
                var newState = _commandHistory.Undo(_store.CurrentState);
                var action = new StateUpdateAction(newState);
                _store.Dispatch(action);
            }
        }

        private void RedoLastCommand()
        {
            if (_commandHistory.CanRedo)
            {
                var newState = _commandHistory.Redo(_store.CurrentState);
                var action = new StateUpdateAction(newState);
                _store.Dispatch(action);
            }
        }

        private void SaveGameState()
        {
            try
            {
                _persistence.SaveState(_store.CurrentState, "savegame");
                Debug.Log("Game state saved successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save game state: {ex.Message}");
            }
        }

        private void LoadGameState()
        {
            try
            {
                var loadedState = _persistence.LoadState("savegame");
                var action = new StateUpdateAction(loadedState);
                _store.Dispatch(action);
                Debug.Log("Game state loaded successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load game state: {ex.Message}");
            }
        }

        private void OnStateChanged(StateChangedEvent<GameState> stateEvent)
        {
            Debug.Log($"State changed by action: {stateEvent.Action}");
            UpdateStateDisplay();
            
            // Check for achievements based on state changes
            CheckAchievements(stateEvent.CurrentState);
        }

        private void OnHistoryChanged(CommandHistoryEvent<GameState> historyEvent)
        {
            Debug.Log($"History changed: {historyEvent.Operation}");
            UpdateHistoryDisplay();
        }

        private void CheckAchievements(GameState state)
        {
            if (state.Level >= 5 && !_unlockedAchievements.Contains("Level Master"))
            {
                _unlockedAchievements.Add("Level Master");
            }

            if (state.Gold >= 1000 && !_unlockedAchievements.Contains("Rich Player"))
            {
                _unlockedAchievements.Add("Rich Player");
            }

            if (_playerInventory.Count >= 10 && !_unlockedAchievements.Contains("Collector"))
            {
                _unlockedAchievements.Add("Collector");
            }
        }

        private void UpdateAllDisplays()
        {
            UpdateStateDisplay();
            UpdateCollectionDisplay();
            UpdateHistoryDisplay();
        }

        private void UpdateStateDisplay()
        {
            if (stateDisplayText != null)
            {
                var state = _store.CurrentState;
                stateDisplayText.text = $"Player: {state.PlayerName}\n" +
                                      $"Level: {state.Level}\n" +
                                      $"Experience: {state.Experience}\n" +
                                      $"Gold: {state.Gold}";
            }
        }

        private void UpdateCollectionDisplay()
        {
            if (collectionDisplayText != null)
            {
                var inventoryText = $"Inventory ({_playerInventory.Count}): {string.Join(", ", _playerInventory)}";
                var statsText = $"Stats: {string.Join(", ", _playerStats.Keys)}";
                var achievementsText = $"Achievements ({_unlockedAchievements.Count}): {string.Join(", ", _unlockedAchievements)}";
                
                collectionDisplayText.text = $"{inventoryText}\n{statsText}\n{achievementsText}";
            }
        }

        private void UpdateHistoryDisplay()
        {
            if (historyDisplayText != null)
            {
                historyDisplayText.text = $"History: {_commandHistory.CurrentIndex}/{_commandHistory.Count}\n" +
                                        $"Can Undo: {_commandHistory.CanUndo}\n" +
                                        $"Can Redo: {_commandHistory.CanRedo}";
            }
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
            _store?.Dispose();
            _commandHistory?.Dispose();
            _playerInventory?.Dispose();
            _playerStats?.Dispose();
            _unlockedAchievements?.Dispose();
            _inventorySynchronizer?.Dispose();
        }
    }

    // Supporting classes for the example

    /// <summary>
    /// Represents the game state for the example.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public string PlayerName;
        public int Level;
        public int Experience;
        public int Gold;

        public override string ToString()
        {
            return $"GameState(Name: {PlayerName}, Level: {Level}, Exp: {Experience}, Gold: {Gold})";
        }
    }

    /// <summary>
    /// Reducer for handling game state changes.
    /// </summary>
    public class GameStateReducer : ReducerBase<GameState, IAction>
    {
        public override GameState Reduce(GameState currentState, IAction action)
        {
            return action switch
            {
                StateUpdateAction updateAction => updateAction.NewState,
                LevelUpAction => currentState.With(state => state.Level, currentState.Level + 1)
                                             .With(state => state.Experience, 0),
                AddGoldAction addGoldAction => currentState.With(state => state.Gold, currentState.Gold + addGoldAction.Amount),
                _ => currentState
            };
        }

        public override bool CanHandle(IAction action)
        {
            return action is StateUpdateAction || action is LevelUpAction || action is AddGoldAction;
        }
    }

    /// <summary>
    /// Action for updating the entire state.
    /// </summary>
    public class StateUpdateAction : ActionBase
    {
        public override string Type => "STATE_UPDATE";
        public GameState NewState { get; }

        public StateUpdateAction(GameState newState)
        {
            NewState = newState;
        }
    }

    /// <summary>
    /// Action for leveling up the player.
    /// </summary>
    public class LevelUpAction : ActionBase
    {
        public override string Type => "LEVEL_UP";
    }

    /// <summary>
    /// Action for adding gold to the player.
    /// </summary>
    public class AddGoldAction : PayloadActionBase<int>
    {
        public override string Type => "ADD_GOLD";
        public int Amount => Payload;

        public AddGoldAction(int amount) : base(amount) { }
    }

    /// <summary>
    /// Command for adding an item to the inventory.
    /// </summary>
    public class AddItemCommand : ReversibleCommandBase<GameState>
    {
        public override string Name => "Add Item";
        private readonly string _item;

        public AddItemCommand(string item)
        {
            _item = item;
        }

        public override GameState Execute(GameState currentState)
        {
            // In a real implementation, you would modify the inventory in the state
            // For this example, we'll just add some gold as a side effect
            return currentState.With(state => state.Gold, currentState.Gold + 10);
        }

        public override GameState Undo(GameState currentState)
        {
            return currentState.With(state => state.Gold, currentState.Gold - 10);
        }
    }

    /// <summary>
    /// Command for removing an item from the inventory.
    /// </summary>
    public class RemoveItemCommand : ReversibleCommandBase<GameState>
    {
        public override string Name => "Remove Item";
        private readonly string _item;
        private readonly int _index;

        public RemoveItemCommand(string item, int index)
        {
            _item = item;
            _index = index;
        }

        public override GameState Execute(GameState currentState)
        {
            // In a real implementation, you would modify the inventory in the state
            // For this example, we'll just remove some gold as a side effect
            return currentState.With(state => state.Gold, Math.Max(0, currentState.Gold - 5));
        }

        public override GameState Undo(GameState currentState)
        {
            return currentState.With(state => state.Gold, currentState.Gold + 5);
        }
    }

    /// <summary>
    /// Command for leveling up the player.
    /// </summary>
    public class LevelUpCommand : ReversibleCommandBase<GameState>
    {
        public override string Name => "Level Up";
        private int _previousLevel;
        private int _previousExperience;

        public override GameState Execute(GameState currentState)
        {
            _previousLevel = currentState.Level;
            _previousExperience = currentState.Experience;

            return currentState.With(state => state.Level, currentState.Level + 1)
                             .With(state => state.Experience, 0);
        }

        public override GameState Undo(GameState currentState)
        {
            return currentState.With(state => state.Level, _previousLevel)
                             .With(state => state.Experience, _previousExperience);
        }
    }
}
