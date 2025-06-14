using System;
using System.Collections.Generic;
using NUnit.Framework;
using Ludo.Reactive.StateManagement;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Tests for the state management system in Ludo.Reactive v1.2.0.
    /// </summary>
    [TestFixture]
    public class StateManagementTests
    {
        private TestState _initialState;
        private TestReducer _reducer;
        private ReactiveStore<TestState> _store;

        [SetUp]
        public void SetUp()
        {
            _initialState = new TestState { Value = 0, Name = "Initial" };
            _reducer = new TestReducer();
            _store = new ReactiveStore<TestState>(_initialState, _reducer);
        }

        [TearDown]
        public void TearDown()
        {
            _store?.Dispose();
        }

        [Test]
        public void ReactiveStore_InitialState_IsCorrect()
        {
            // Assert
            Assert.AreEqual(_initialState.Value, _store.CurrentState.Value);
            Assert.AreEqual(_initialState.Name, _store.CurrentState.Name);
        }

        [Test]
        public void ReactiveStore_DispatchAction_UpdatesState()
        {
            // Arrange
            var action = new TestAction { NewValue = 42 };

            // Act
            var newState = _store.Dispatch(action);

            // Assert
            Assert.AreEqual(42, newState.Value);
            Assert.AreEqual(42, _store.CurrentState.Value);
        }

        [Test]
        public void ReactiveStore_StateChanged_EmitsEvent()
        {
            // Arrange
            StateChangedEvent<TestState>? receivedEvent = null;
            _store.StateChanged.Subscribe(evt => receivedEvent = evt);
            var action = new TestAction { NewValue = 100 };

            // Act
            _store.Dispatch(action);

            // Assert
            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(0, receivedEvent.Value.PreviousState.Value);
            Assert.AreEqual(100, receivedEvent.Value.CurrentState.Value);
            Assert.AreEqual(action, receivedEvent.Value.Action);
        }

        [Test]
        public void ReactiveStore_Subscribe_EmitsCurrentState()
        {
            // Arrange
            TestState? receivedState = null;
            
            // Act
            _store.Subscribe(state => receivedState = state);

            // Assert
            Assert.IsNotNull(receivedState);
            Assert.AreEqual(_initialState.Value, receivedState.Value);
        }

        [Test]
        public void ReactiveStore_Select_CreatesReactiveSelector()
        {
            // Arrange
            var selector = _store.Select(state => state.Value, "ValueSelector");
            var receivedValues = new List<int>();
            selector.Subscribe(value => receivedValues.Add(value));

            // Act
            _store.Dispatch(new TestAction { NewValue = 10 });
            _store.Dispatch(new TestAction { NewValue = 20 });

            // Assert
            Assert.AreEqual(3, receivedValues.Count); // Initial + 2 changes
            Assert.AreEqual(0, receivedValues[0]);
            Assert.AreEqual(10, receivedValues[1]);
            Assert.AreEqual(20, receivedValues[2]);
        }

        [Test]
        public void MemoizedSelector_CachesResults()
        {
            // Arrange
            var callCount = 0;
            var selector = new MemoizedSelector<TestState, string>(
                state => { callCount++; return $"Value: {state.Value}"; },
                "TestSelector");

            // Act
            var result1 = selector.Select(_initialState);
            var result2 = selector.Select(_initialState);
            var result3 = selector.Select(new TestState { Value = 1, Name = "Different" });

            // Assert
            Assert.AreEqual("Value: 0", result1);
            Assert.AreEqual("Value: 0", result2);
            Assert.AreEqual("Value: 1", result3);
            Assert.AreEqual(2, callCount); // Should only be called twice due to caching
            Assert.AreEqual(1, selector.CacheHitCount);
        }

        [Test]
        public void CommandHistory_ExecuteCommand_AddsToHistory()
        {
            // Arrange
            var history = new CommandHistory<TestState>();
            var command = new TestCommand(42);

            // Act
            var newState = history.ExecuteCommand(command, _initialState);

            // Assert
            Assert.AreEqual(42, newState.Value);
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual(1, history.CurrentIndex);
            Assert.IsTrue(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
        }

        [Test]
        public void CommandHistory_Undo_RestoresPreviousState()
        {
            // Arrange
            var history = new CommandHistory<TestState>();
            var command = new TestCommand(42);
            var newState = history.ExecuteCommand(command, _initialState);

            // Act
            var undoState = history.Undo(newState);

            // Assert
            Assert.AreEqual(0, undoState.Value);
            Assert.AreEqual(0, history.CurrentIndex);
            Assert.IsFalse(history.CanUndo);
            Assert.IsTrue(history.CanRedo);
        }

        [Test]
        public void CommandHistory_Redo_ReappliesCommand()
        {
            // Arrange
            var history = new CommandHistory<TestState>();
            var command = new TestCommand(42);
            var newState = history.ExecuteCommand(command, _initialState);
            var undoState = history.Undo(newState);

            // Act
            var redoState = history.Redo(undoState);

            // Assert
            Assert.AreEqual(42, redoState.Value);
            Assert.AreEqual(1, history.CurrentIndex);
            Assert.IsTrue(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
        }

        [Test]
        public void ImmutableStateUpdater_UpdateProperty_CreatesNewInstance()
        {
            // Arrange
            var originalState = new TestState { Value = 10, Name = "Original" };

            // Act
            var newState = ImmutableStateUpdater.UpdateProperty(
                originalState, 
                state => state.Value, 
                20);

            // Assert
            Assert.AreEqual(10, originalState.Value); // Original unchanged
            Assert.AreEqual(20, newState.Value); // New state updated
            Assert.AreEqual("Original", newState.Name); // Other properties preserved
        }

        [Test]
        public void StatePersistence_SaveAndLoad_WorksCorrectly()
        {
            // Arrange
            var persistence = new MemoryStatePersistence<TestState>();
            var state = new TestState { Value = 123, Name = "Test" };

            // Act
            persistence.SaveState(state, "test");
            var loadedState = persistence.LoadState("test");

            // Assert
            Assert.AreEqual(state.Value, loadedState.Value);
            Assert.AreEqual(state.Name, loadedState.Name);
        }

        [Test]
        public void StatePersistence_HasState_ReturnsCorrectValue()
        {
            // Arrange
            var persistence = new MemoryStatePersistence<TestState>();
            var state = new TestState { Value = 123, Name = "Test" };

            // Act & Assert
            Assert.IsFalse(persistence.HasState("test"));
            persistence.SaveState(state, "test");
            Assert.IsTrue(persistence.HasState("test"));
        }

        [Test]
        public void StatePersistence_DeleteState_RemovesState()
        {
            // Arrange
            var persistence = new MemoryStatePersistence<TestState>();
            var state = new TestState { Value = 123, Name = "Test" };
            persistence.SaveState(state, "test");

            // Act
            persistence.DeleteState("test");

            // Assert
            Assert.IsFalse(persistence.HasState("test"));
        }
    }

    // Test helper classes
    public class TestState
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }

    public class TestAction : ActionBase
    {
        public override string Type => "TEST_ACTION";
        public int NewValue { get; set; }
    }

    public class TestReducer : ReducerBase<TestState, IAction>
    {
        public override TestState Reduce(TestState currentState, IAction action)
        {
            return action switch
            {
                TestAction testAction => new TestState { Value = testAction.NewValue, Name = currentState.Name },
                _ => currentState
            };
        }

        public override bool CanHandle(IAction action)
        {
            return action is TestAction;
        }
    }

    public class TestCommand : ReversibleCommandBase<TestState>
    {
        public override string Name => "Test Command";
        private readonly int _newValue;
        private int _oldValue;

        public TestCommand(int newValue)
        {
            _newValue = newValue;
        }

        public override TestState Execute(TestState currentState)
        {
            _oldValue = currentState.Value;
            return new TestState { Value = _newValue, Name = currentState.Name };
        }

        public override TestState Undo(TestState currentState)
        {
            return new TestState { Value = _oldValue, Name = currentState.Name };
        }
    }
}
