using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Unit tests for TestScheduler.
    /// </summary>
    public class TestSchedulerTests
    {
        private TestScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new TestScheduler();
        }

        [TearDown]
        public void TearDown()
        {
            _scheduler?.Dispose();
        }

        [Test]
        public void TestScheduler_InitialTime_IsZero()
        {
            // Assert
            Assert.AreEqual(0, _scheduler.CurrentTime);
            Assert.AreEqual(new DateTimeOffset(0, TimeSpan.Zero), _scheduler.Now);
        }

        [Test]
        public void TestScheduler_AdvanceBy_UpdatesCurrentTime()
        {
            // Act
            _scheduler.AdvanceBy(TimeSpan.FromSeconds(10));

            // Assert
            Assert.AreEqual(TimeSpan.FromSeconds(10).Ticks, _scheduler.CurrentTime);
        }

        [Test]
        public void TestScheduler_AdvanceTo_UpdatesCurrentTime()
        {
            // Arrange
            var targetTime = TimeSpan.FromMinutes(5).Ticks;

            // Act
            _scheduler.AdvanceTo(targetTime);

            // Assert
            Assert.AreEqual(targetTime, _scheduler.CurrentTime);
        }

        [Test]
        public void TestScheduler_ScheduleImmediate_ExecutesImmediately()
        {
            // Arrange
            var executed = false;
            
            // Act
            _scheduler.Schedule(Unit.Default, (scheduler, state) =>
            {
                executed = true;
                return Disposable.Empty;
            });

            // Assert
            Assert.IsTrue(executed);
        }

        [Test]
        public void TestScheduler_ScheduleDelayed_ExecutesAfterAdvance()
        {
            // Arrange
            var executed = false;
            var delay = TimeSpan.FromSeconds(5);
            
            // Act
            _scheduler.Schedule(Unit.Default, delay, (scheduler, state) =>
            {
                executed = true;
                return Disposable.Empty;
            });

            // Assert - Should not execute immediately
            Assert.IsFalse(executed);

            // Advance time and check execution
            _scheduler.AdvanceBy(delay);
            Assert.IsTrue(executed);
        }

        [Test]
        public void TestScheduler_ScheduleMultiple_ExecutesInOrder()
        {
            // Arrange
            var executionOrder = new List<int>();
            
            // Act - Schedule in reverse order
            _scheduler.Schedule(3, TimeSpan.FromSeconds(3), (scheduler, state) =>
            {
                executionOrder.Add(state);
                return Disposable.Empty;
            });
            
            _scheduler.Schedule(1, TimeSpan.FromSeconds(1), (scheduler, state) =>
            {
                executionOrder.Add(state);
                return Disposable.Empty;
            });
            
            _scheduler.Schedule(2, TimeSpan.FromSeconds(2), (scheduler, state) =>
            {
                executionOrder.Add(state);
                return Disposable.Empty;
            });

            // Advance time to execute all
            _scheduler.AdvanceBy(TimeSpan.FromSeconds(5));

            // Assert - Should execute in time order
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual(1, executionOrder[0]);
            Assert.AreEqual(2, executionOrder[1]);
            Assert.AreEqual(3, executionOrder[2]);
        }

        [Test]
        public void TestScheduler_SchedulePeriodic_ExecutesRepeatedly()
        {
            // Arrange
            var executionCount = 0;
            var period = TimeSpan.FromSeconds(1);
            
            // Act
            var subscription = _scheduler.SchedulePeriodic(Unit.Default, period, state =>
            {
                executionCount++;
                return state;
            });

            // Advance time to trigger multiple executions
            _scheduler.AdvanceBy(TimeSpan.FromSeconds(3.5));

            // Assert - Should execute 3 times (at 1s, 2s, 3s)
            Assert.AreEqual(3, executionCount);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void TestScheduler_CancelScheduledAction_PreventsExecution()
        {
            // Arrange
            var executed = false;
            
            // Act
            var subscription = _scheduler.Schedule(Unit.Default, TimeSpan.FromSeconds(5), (scheduler, state) =>
            {
                executed = true;
                return Disposable.Empty;
            });

            subscription.Dispose(); // Cancel before execution
            _scheduler.AdvanceBy(TimeSpan.FromSeconds(10));

            // Assert
            Assert.IsFalse(executed);
        }

        [Test]
        public void TestScheduler_Start_ExecutesAllScheduledItems()
        {
            // Arrange
            var executionOrder = new List<int>();
            
            _scheduler.Schedule(1, TimeSpan.FromSeconds(1), (scheduler, state) =>
            {
                executionOrder.Add(state);
                return Disposable.Empty;
            });
            
            _scheduler.Schedule(2, TimeSpan.FromSeconds(2), (scheduler, state) =>
            {
                executionOrder.Add(state);
                return Disposable.Empty;
            });

            // Act
            _scheduler.Start();

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual(1, executionOrder[0]);
            Assert.AreEqual(2, executionOrder[1]);
            Assert.AreEqual(TimeSpan.FromSeconds(2).Ticks, _scheduler.CurrentTime);
        }

        [Test]
        public void TestScheduler_GetScheduledItems_ReturnsCorrectItems()
        {
            // Arrange
            _scheduler.Schedule(1, TimeSpan.FromSeconds(1), (scheduler, state) => Disposable.Empty);
            _scheduler.Schedule(2, TimeSpan.FromSeconds(2), (scheduler, state) => Disposable.Empty);

            // Act
            var items = _scheduler.GetScheduledItems();

            // Assert
            Assert.AreEqual(2, items.Count);
        }

        [Test]
        public void TestScheduler_Clear_RemovesAllScheduledItems()
        {
            // Arrange
            _scheduler.Schedule(1, TimeSpan.FromSeconds(1), (scheduler, state) => Disposable.Empty);
            _scheduler.Schedule(2, TimeSpan.FromSeconds(2), (scheduler, state) => Disposable.Empty);

            // Act
            _scheduler.Clear();

            // Assert
            Assert.AreEqual(0, _scheduler.GetScheduledItems().Count);
        }

        [Test]
        public void TestScheduler_AdvanceToNegativeTime_ThrowsException()
        {
            // Arrange
            _scheduler.AdvanceBy(TimeSpan.FromSeconds(10));

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _scheduler.AdvanceTo(0));
        }

        [Test]
        public void TestScheduler_AdvanceByNegativeTime_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _scheduler.AdvanceBy(TimeSpan.FromSeconds(-1)));
        }

        [Test]
        public void TestScheduler_SchedulePeriodicWithNegativePeriod_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                _scheduler.SchedulePeriodic(Unit.Default, TimeSpan.FromSeconds(-1), state => state));
        }

        [Test]
        public void TestScheduler_ScheduleWithNullAction_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _scheduler.Schedule<Unit>(Unit.Default, null));
        }

        [Test]
        public void TestScheduler_PeriodicWithStateUpdate_UpdatesState()
        {
            // Arrange
            var finalState = 0;
            var period = TimeSpan.FromSeconds(1);

            // Act
            var subscription = _scheduler.SchedulePeriodic(0, period, state =>
            {
                var newState = state + 1;
                finalState = newState;  // Capture the new state, not the input state
                return newState;
            });

            // Advance time to trigger multiple executions
            _scheduler.AdvanceBy(TimeSpan.FromSeconds(3.5));

            // Assert - State should be updated each time
            Assert.AreEqual(3, finalState);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void TestScheduler_ExceptionInScheduledAction_DoesNotBreakScheduler()
        {
            // Arrange
            var executedAfterException = false;

            // Temporarily ignore failing messages for this test
            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                // Schedule action that throws
                _scheduler.Schedule(Unit.Default, TimeSpan.FromSeconds(1), (scheduler, state) =>
                {
                    throw new Exception("Test exception");
                });

                // Schedule action that should still execute
                _scheduler.Schedule(Unit.Default, TimeSpan.FromSeconds(2), (scheduler, state) =>
                {
                    executedAfterException = true;
                    return Disposable.Empty;
                });

                // Act
                _scheduler.AdvanceBy(TimeSpan.FromSeconds(3));

                // Assert
                Assert.IsTrue(executedAfterException);
            }
            finally
            {
                // Restore previous setting
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
        }

        [Test]
        public void TestScheduler_Dispose_CleansUpResources()
        {
            // Arrange
            _scheduler.Schedule(Unit.Default, TimeSpan.FromSeconds(1), (scheduler, state) => Disposable.Empty);

            // Act
            _scheduler.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => _scheduler.AdvanceBy(TimeSpan.FromSeconds(1)));
            Assert.Throws<ObjectDisposedException>(() => _scheduler.Schedule(Unit.Default, (scheduler, state) => Disposable.Empty));
        }

        [Test]
        public void TestScheduler_ScheduleExtensionMethods_Work()
        {
            // Arrange
            var executed = false;
            
            // Act
            _scheduler.Schedule(() => executed = true);

            // Assert
            Assert.IsTrue(executed);
        }

        [Test]
        public void TestScheduler_ScheduleDelayedExtensionMethods_Work()
        {
            // Arrange
            var executed = false;
            var delay = TimeSpan.FromSeconds(5);
            
            // Act
            _scheduler.Schedule(delay, () => executed = true);
            _scheduler.AdvanceBy(delay);

            // Assert
            Assert.IsTrue(executed);
        }

        [Test]
        public void TestScheduler_SchedulePeriodicExtensionMethods_Work()
        {
            // Arrange
            var executionCount = 0;
            var period = TimeSpan.FromSeconds(1);
            
            // Act
            var subscription = _scheduler.SchedulePeriodic(period, () => executionCount++);
            _scheduler.AdvanceBy(TimeSpan.FromSeconds(2.5));

            // Assert
            Assert.AreEqual(2, executionCount);

            // Cleanup
            subscription.Dispose();
        }
    }
}
