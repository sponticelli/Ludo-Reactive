using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ludo.Reactive.Tests
{
    /// <summary>
    /// Unit tests for MessageBroker system.
    /// </summary>
    public class MessageBrokerTests
    {
        private MessageBroker _messageBroker;

        [SetUp]
        public void SetUp()
        {
            _messageBroker = new MessageBroker();
        }

        [TearDown]
        public void TearDown()
        {
            _messageBroker?.Dispose();
        }

        [Test]
        public void MessageBroker_PublishAndSubscribe_Works()
        {
            // Arrange
            var receivedMessages = new List<string>();
            var subscription = _messageBroker.Subscribe<string>(msg => receivedMessages.Add(msg));

            // Act
            _messageBroker.Publish("Hello");
            _messageBroker.Publish("World");

            // Assert
            Assert.AreEqual(2, receivedMessages.Count);
            Assert.AreEqual("Hello", receivedMessages[0]);
            Assert.AreEqual("World", receivedMessages[1]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void MessageBroker_MultipleSubscribers_AllReceiveMessages()
        {
            // Arrange
            var receivedMessages1 = new List<int>();
            var receivedMessages2 = new List<int>();
            
            var subscription1 = _messageBroker.Subscribe<int>(msg => receivedMessages1.Add(msg));
            var subscription2 = _messageBroker.Subscribe<int>(msg => receivedMessages2.Add(msg));

            // Act
            _messageBroker.Publish(42);
            _messageBroker.Publish(84);

            // Assert
            Assert.AreEqual(2, receivedMessages1.Count);
            Assert.AreEqual(2, receivedMessages2.Count);
            Assert.AreEqual(42, receivedMessages1[0]);
            Assert.AreEqual(84, receivedMessages1[1]);
            Assert.AreEqual(42, receivedMessages2[0]);
            Assert.AreEqual(84, receivedMessages2[1]);

            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }

        [Test]
        public void MessageBroker_FilteredSubscription_OnlyReceivesFilteredMessages()
        {
            // Arrange
            var receivedMessages = new List<int>();
            var subscription = _messageBroker.Subscribe<int>(x => x > 10, msg => receivedMessages.Add(msg));

            // Act
            _messageBroker.Publish(5);
            _messageBroker.Publish(15);
            _messageBroker.Publish(8);
            _messageBroker.Publish(20);

            // Assert
            Assert.AreEqual(2, receivedMessages.Count);
            Assert.AreEqual(15, receivedMessages[0]);
            Assert.AreEqual(20, receivedMessages[1]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void MessageBroker_TypeSafety_DifferentTypesIsolated()
        {
            // Arrange
            var stringMessages = new List<string>();
            var intMessages = new List<int>();
            
            var stringSubscription = _messageBroker.Subscribe<string>(msg => stringMessages.Add(msg));
            var intSubscription = _messageBroker.Subscribe<int>(msg => intMessages.Add(msg));

            // Act
            _messageBroker.Publish("Hello");
            _messageBroker.Publish(42);
            _messageBroker.Publish("World");

            // Assert
            Assert.AreEqual(2, stringMessages.Count);
            Assert.AreEqual(1, intMessages.Count);
            Assert.AreEqual("Hello", stringMessages[0]);
            Assert.AreEqual("World", stringMessages[1]);
            Assert.AreEqual(42, intMessages[0]);

            // Cleanup
            stringSubscription.Dispose();
            intSubscription.Dispose();
        }

        [Test]
        public void MessageBroker_Unsubscribe_StopsReceivingMessages()
        {
            // Arrange
            var receivedMessages = new List<string>();
            var subscription = _messageBroker.Subscribe<string>(msg => receivedMessages.Add(msg));

            // Act
            _messageBroker.Publish("Before");
            subscription.Dispose();
            _messageBroker.Publish("After");

            // Assert
            Assert.AreEqual(1, receivedMessages.Count);
            Assert.AreEqual("Before", receivedMessages[0]);
        }

        [Test]
        public void MessageBroker_ErrorHandling_DoesNotBreakOtherSubscribers()
        {
            // Arrange
            var receivedMessages = new List<string>();
            var errorSubscription = _messageBroker.Subscribe<string>(msg => throw new Exception("Test error"));
            var normalSubscription = _messageBroker.Subscribe<string>(msg => receivedMessages.Add(msg));

            // Temporarily ignore failing messages for this test
            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                // Act
                _messageBroker.Publish("Test");

                // Assert
                Assert.AreEqual(1, receivedMessages.Count);
                Assert.AreEqual("Test", receivedMessages[0]);
            }
            finally
            {
                // Restore previous setting
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;

                // Cleanup
                errorSubscription.Dispose();
                normalSubscription.Dispose();
            }
        }

        [Test]
        public void MessageBroker_GetObservable_ReturnsObservableSequence()
        {
            // Arrange
            var receivedMessages = new List<string>();
            var observable = _messageBroker.GetObservable<string>();
            var subscription = observable.Subscribe(msg => receivedMessages.Add(msg));

            // Act
            _messageBroker.Publish("Observable Test");

            // Assert
            Assert.AreEqual(1, receivedMessages.Count);
            Assert.AreEqual("Observable Test", receivedMessages[0]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void MessageBroker_Clear_RemovesAllSubscriptionsForType()
        {
            // Arrange
            var stringMessages = new List<string>();
            var intMessages = new List<int>();
            
            var stringSubscription = _messageBroker.Subscribe<string>(msg => stringMessages.Add(msg));
            var intSubscription = _messageBroker.Subscribe<int>(msg => intMessages.Add(msg));

            // Act
            _messageBroker.Publish("Before Clear");
            _messageBroker.Publish(42);
            _messageBroker.Clear<string>();
            _messageBroker.Publish("After Clear");
            _messageBroker.Publish(84);

            // Assert
            Assert.AreEqual(1, stringMessages.Count);
            Assert.AreEqual(2, intMessages.Count);
            Assert.AreEqual("Before Clear", stringMessages[0]);
            Assert.AreEqual(42, intMessages[0]);
            Assert.AreEqual(84, intMessages[1]);

            // Cleanup
            stringSubscription.Dispose();
            intSubscription.Dispose();
        }

        [Test]
        public void MessageBroker_ClearAll_RemovesAllSubscriptions()
        {
            // Arrange
            var stringMessages = new List<string>();
            var intMessages = new List<int>();
            
            var stringSubscription = _messageBroker.Subscribe<string>(msg => stringMessages.Add(msg));
            var intSubscription = _messageBroker.Subscribe<int>(msg => intMessages.Add(msg));

            // Act
            _messageBroker.Publish("Before Clear");
            _messageBroker.Publish(42);
            _messageBroker.ClearAll();
            _messageBroker.Publish("After Clear");
            _messageBroker.Publish(84);

            // Assert
            Assert.AreEqual(1, stringMessages.Count);
            Assert.AreEqual(1, intMessages.Count);
            Assert.AreEqual("Before Clear", stringMessages[0]);
            Assert.AreEqual(42, intMessages[0]);

            // Cleanup
            stringSubscription.Dispose();
            intSubscription.Dispose();
        }

        [Test]
        public void MessageBroker_GetSubscriptionCount_ReturnsCorrectCount()
        {
            // Arrange
            var subscription1 = _messageBroker.Subscribe<string>(msg => { });
            var subscription2 = _messageBroker.Subscribe<string>(msg => { });

            // Act & Assert
            Assert.AreEqual(2, _messageBroker.GetSubscriptionCount<string>());
            Assert.AreEqual(0, _messageBroker.GetSubscriptionCount<int>());

            subscription1.Dispose();
            Assert.AreEqual(1, _messageBroker.GetSubscriptionCount<string>());

            subscription2.Dispose();
            Assert.AreEqual(0, _messageBroker.GetSubscriptionCount<string>());
        }

        [Test]
        public void MessageBroker_GlobalInstance_Works()
        {
            // Arrange
            var receivedMessages = new List<string>();
            var subscription = MessageBroker.Global.Subscribe<string>(msg => receivedMessages.Add(msg));

            // Act
            MessageBroker.Global.Publish("Global Test");

            // Assert
            Assert.AreEqual(1, receivedMessages.Count);
            Assert.AreEqual("Global Test", receivedMessages[0]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void MessageBrokerExtensions_PublishExtension_Works()
        {
            // Arrange
            var receivedMessages = new List<string>();
            var subscription = MessageBroker.Global.Subscribe<string>(msg => receivedMessages.Add(msg));

            // Act
            "Extension Test".Publish();

            // Assert
            Assert.AreEqual(1, receivedMessages.Count);
            Assert.AreEqual("Extension Test", receivedMessages[0]);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void MessageBroker_Dispose_CleansUpResources()
        {
            // Arrange
            var receivedMessages = new List<string>();
            var subscription = _messageBroker.Subscribe<string>(msg => receivedMessages.Add(msg));

            // Act
            _messageBroker.Publish("Before Dispose");
            _messageBroker.Dispose();

            // Assert
            Assert.AreEqual(1, receivedMessages.Count);
            Assert.Throws<ObjectDisposedException>(() => _messageBroker.Publish("After Dispose"));

            // Cleanup
            subscription.Dispose();
        }

        // Custom message types for testing
        public class TestMessage
        {
            public string Content { get; set; }
            public int Value { get; set; }
        }

        [Test]
        public void MessageBroker_CustomMessageType_Works()
        {
            // Arrange
            var receivedMessages = new List<TestMessage>();
            var subscription = _messageBroker.Subscribe<TestMessage>(msg => receivedMessages.Add(msg));

            // Act
            var testMessage = new TestMessage { Content = "Test", Value = 123 };
            _messageBroker.Publish(testMessage);

            // Assert
            Assert.AreEqual(1, receivedMessages.Count);
            Assert.AreEqual("Test", receivedMessages[0].Content);
            Assert.AreEqual(123, receivedMessages[0].Value);

            // Cleanup
            subscription.Dispose();
        }
    }
}
