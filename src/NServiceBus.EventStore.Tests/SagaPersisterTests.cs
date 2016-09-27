using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Extensibility;
using NServiceBus.Sagas;
using NServiceBus.Transport;
using NUnit.Framework;

namespace NServiceBus.EventStore.Tests
{
    [TestFixture]
    public class SagaPersisterTests
    {
        IEventStoreConnection connection;
        SagaPersister persister;
        const string CorrelationPropName = "StringProperty";

        [Test]
        public async Task When_a_duplicate_messages_starting_a_saga_arrive_simultanously_one_of_them_fails()
        {
            var context = CreateMessageContext();
            var session1 = new EventStoreSynchronizedStorageSession(connection);

            var sagaData = CreateNewSagaData();
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session1, context);

            var session2 = new EventStoreSynchronizedStorageSession(connection);

            try
            {
                await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session2, context);
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                StringAssert.StartsWith("Append failed due to WrongExpectedVersion.", ex.Message);
            }
        }

        [Test]
        public async Task When_a_duplicate_messages_starting_a_saga_arrive_sequentially_both_succeed()
        {
            var context = CreateMessageContext();
            var sagaData = CreateNewSagaData();

            var session1 = new EventStoreSynchronizedStorageSession(connection);
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session1, context);

            var session2 = new EventStoreSynchronizedStorageSession(connection);
            var loadedData = await persister.Get<SagaData>(CorrelationPropName, sagaData.StringProperty, session2, context);
            await persister.Update(loadedData, session2, context);
        }

        [Test]
        public async Task When_a_two_messages_starting_a_saga_arrive_simultanously_one_of_them_fails()
        {
            var context1 = CreateMessageContext();
            var context2 = CreateMessageContext();

            var session1 = new EventStoreSynchronizedStorageSession(connection);

            var sagaData = CreateNewSagaData();
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session1, context1);

            var session2 = new EventStoreSynchronizedStorageSession(connection);

            try
            {
                await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session2, context2);
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                StringAssert.StartsWith("Append failed due to WrongExpectedVersion.", ex.Message);
            }
        }

        [Test]
        public async Task When_a_two_messages_starting_a_saga_arrive_sequentially_both_succeed()
        {
            var context1 = CreateMessageContext();
            var context2 = CreateMessageContext();
            var sagaData = CreateNewSagaData();

            var session1 = new EventStoreSynchronizedStorageSession(connection);
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session1, context1);

            var session2 = new EventStoreSynchronizedStorageSession(connection);
            var loadedData = await persister.Get<SagaData>(CorrelationPropName, sagaData.StringProperty, session2, context2);
            await persister.Update(loadedData, session2, context2);
        }

        class SagaData : ContainSagaData
        {
            public string StringProperty { get; set; }
            public int Value { get; set; }
        }

        [SetUp]
        public void Connect()
        {
            connection = EventStoreConnection.Create(new IPEndPoint(IPAddress.Loopback, 1113));
            connection.ConnectAsync().GetAwaiter().GetResult();

            persister = new SagaPersister();
        }

        ContextBag CreateMessageContext()
        {
            var messageId = Guid.NewGuid().ToString("N");
            var contextBag = new ContextBag();
            var incomingMessage = new IncomingMessage(messageId, new Dictionary<string, string>(), new byte[0]);
            contextBag.Set(incomingMessage);
            var transaction = new TransportTransaction();
            transaction.Set(connection);
            contextBag.Set(transaction);
            return contextBag;
        }

        static SagaData CreateNewSagaData()
        {
            return new SagaData
            {
                Id = Guid.NewGuid(),
                StringProperty = Guid.NewGuid().ToString("N"),
                Value = 1
            };
        }

        [TearDown]
        public void Disconnect()
        {
            connection.Dispose();
        }
    }
}