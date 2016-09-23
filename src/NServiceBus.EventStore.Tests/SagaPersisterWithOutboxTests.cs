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
    public class SagaPersisterWithOutboxTests
    {
        IEventStoreConnection connection;
        SagaPersister persister;
        OutboxPersister outboxPersister;
        const string CorrelationPropName = "StringProperty";

        [Test]
        public async Task When_a_duplicate_messages_starting_a_saga_arrive_simultanously_one_of_them_fails()
        {
            var context = CreateMessageContext();
            var transaction1 = await outboxPersister.BeginTransaction(context);
            var session1 = new OutboxEventStoreSynchronizedStorageSession(connection, (EventStoreOutboxTransaction)transaction1);

            var sagaData = CreateNewSagaData();
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session1, context);

            var transaction2 = await outboxPersister.BeginTransaction(context);
            var session2 = new OutboxEventStoreSynchronizedStorageSession(connection, (EventStoreOutboxTransaction)transaction2);

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
        public async Task When_a_duplicate_messages_starting_a_saga_arrive_sequentially_one_of_them_fails()
        {
            var context = CreateMessageContext();
            var sagaData = CreateNewSagaData();

            var transaction1 = await outboxPersister.BeginTransaction(context);
            var session1 = new OutboxEventStoreSynchronizedStorageSession(connection, (EventStoreOutboxTransaction)transaction1);
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session1, context);
            await transaction1.Commit();

            var transaction2 = await outboxPersister.BeginTransaction(context);
            var session2 = new OutboxEventStoreSynchronizedStorageSession(connection, (EventStoreOutboxTransaction)transaction2);
            var loadedData = await persister.Get<SagaData>(CorrelationPropName, sagaData.StringProperty, session2, context);
            await persister.Update(loadedData, session2, context);

            try
            {
                await transaction2.Commit();
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                StringAssert.StartsWith("Append failed due to WrongExpectedVersion.", ex.Message);
            }
        }

        [Test]
        public async Task When_a_two_messages_starting_a_saga_arrive_simultanously_one_of_them_fails()
        {
            var context1 = CreateMessageContext();
            var context2 = CreateMessageContext();
            var transaction1 = await outboxPersister.BeginTransaction(context1);
            var session1 = new OutboxEventStoreSynchronizedStorageSession(connection, (EventStoreOutboxTransaction)transaction1);

            var sagaData = CreateNewSagaData();
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session1, context1);

            var transaction2 = await outboxPersister.BeginTransaction(context2);
            var session2 = new OutboxEventStoreSynchronizedStorageSession(connection, (EventStoreOutboxTransaction)transaction2);

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

            var transaction1 = await outboxPersister.BeginTransaction(context1);
            var session1 = new OutboxEventStoreSynchronizedStorageSession(connection, (EventStoreOutboxTransaction)transaction1);
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), session1, context1);
            await transaction1.Commit();

            var transaction2 = await outboxPersister.BeginTransaction(context2);
            var session2 = new OutboxEventStoreSynchronizedStorageSession(connection, (EventStoreOutboxTransaction)transaction2);
            var loadedData = await persister.Get<SagaData>(CorrelationPropName, sagaData.StringProperty, session2, context2);
            await persister.Update(loadedData, session2, context2);
            await transaction2.Commit();
        }

        [Test]
        public async Task When_using_outbox_saga_state_is_reconstituted_from_linked_events()
        {
            var sagaData = CreateNewSagaData();
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), new EventStoreSynchronizedStorageSession(connection), CreateMessageContext());

            //Process first message
            await SimulateProcessingMessage(sagaData, d => { d.Value = 2; }, true, CreateMessageContext());

            //Verify state
            await SimulateProcessingMessage(sagaData, d =>
            {
                Assert.AreEqual(2, d.Value);
            }, true, CreateMessageContext());
        }

        [Test]
        public async Task When_using_outbox_and_message_fails_the_subsequent_messages_fails_to()
        {
            var sagaData = CreateNewSagaData();
            await persister.Save(sagaData, new SagaCorrelationProperty(CorrelationPropName, sagaData.StringProperty), new EventStoreSynchronizedStorageSession(connection), CreateMessageContext());

            //Fail first message
            var failingMessageContext = CreateMessageContext();
            await SimulateProcessingMessage(sagaData, d => { d.Value = 2; }, false, failingMessageContext);

            try
            {
                await SimulateProcessingMessage(sagaData, d =>
                {
                    Assert.AreEqual(1, d.Value);
                }, true, CreateMessageContext());
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.Message, "A previous message destined for this saga has failed. Triggering retries.");
            }

            //Process the first message correctly for the second time.
            await SimulateProcessingMessage(sagaData, d => { d.Value = 2; }, true, failingMessageContext);
        }

        async Task SimulateProcessingMessage(SagaData sagaData, Action<SagaData> action, bool commitOutboxTransaction, ContextBag context)
        {
            var outboxTransaction = await outboxPersister.BeginTransaction(context);
            var session = new OutboxEventStoreSynchronizedStorageSession(connection, (EventStoreOutboxTransaction)outboxTransaction);
            sagaData = await persister.Get<SagaData>(CorrelationPropName, sagaData.StringProperty, session, context);
            action(sagaData);
            await persister.Update(sagaData, session, context);
            if (commitOutboxTransaction)
            {
                await outboxTransaction.Commit();
            }
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
            outboxPersister = new OutboxPersister(null);
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