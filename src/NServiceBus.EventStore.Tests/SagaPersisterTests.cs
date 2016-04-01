using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Persistence.EventStore.SagaPersister;
using NServiceBus.Sagas;
using NServiceBus.Transports;
using NUnit.Framework;

namespace NServiceBus.EventStore.Tests
{
    [TestFixture]
    public class SagaPersisterTests
    {
        IEventStoreConnection connection;

        [Test]
        public async Task It_saves_saga_data_and_creates_index()
        {
            var messageId = Guid.NewGuid().ToString();
            var correlationProperty = Guid.NewGuid().ToString("N");

            var persister = new SagaPersister();
            
            var sagaId = await ProcessMessage(messageId, null, correlationProperty, int.MaxValue, new List<Guid>()).ConfigureAwait(false);;

            var context = CreateContext(messageId);
            var loadedById = await persister.Get<SagaData>(sagaId.Value, new EventStoreSynchronizedStorageSession(connection), context).ConfigureAwait(false);
            var loadedByCorrelationProp = await persister.Get<SagaData>("StringProperty", correlationProperty, new EventStoreSynchronizedStorageSession(connection), context).ConfigureAwait(false);

            Assert.IsNotNull(loadedById);
            Assert.IsNotNull(loadedByCorrelationProp);
        }


        [Test]
        public async Task It_prevents_creating_two_instances_when_concurrently_trying_to_save()
        {
            var firstMessageId = Guid.NewGuid().ToString();
            var secondMessageId = Guid.NewGuid().ToString();
            var correlationProperty = Guid.NewGuid().ToString("N");
            var outgoingMessages = new List<Guid>();

            await ProcessMessage(firstMessageId, null, correlationProperty, 2, outgoingMessages).ConfigureAwait(false);
            try
            {
                await ProcessMessage(secondMessageId, null, correlationProperty, int.MaxValue, outgoingMessages).ConfigureAwait(false);
                Assert.Fail();
            }
            catch (Exception)
            {
                Assert.Pass();
            }
        }

        [Test]
        public async Task It_saves_saga_data_if_previously_only_index_has_been_created()
        {
            var messageId = Guid.NewGuid().ToString();
            var correlationProperty = Guid.NewGuid().ToString("N");

            var outgoingMessages = new List<Guid>();
            await ProcessMessage(messageId, null, correlationProperty, 2, outgoingMessages).ConfigureAwait(false);
            var sagaId = await ProcessMessage(messageId, null, correlationProperty, int.MaxValue, outgoingMessages).ConfigureAwait(false);

            var persister = new SagaPersister();
            var context = CreateContext(messageId);
            var loadedById = await persister.Get<SagaData>(sagaId.Value, new EventStoreSynchronizedStorageSession(connection), context).ConfigureAwait(false);

            Assert.IsNotNull(loadedById);
            Assert.IsTrue(outgoingMessages.Distinct().Count() == 1);
        }

        static ContextBag CreateContext(string messageId)
        {
            var context = new ContextBag();
            context.Set(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()));
            return context;
        }

        async Task<Guid?> ProcessMessage(string messageId, Guid? sagaId, string correlationProperty, int successfulCalls, List<Guid> outgoingMessages)
        {
            var persister = new SagaPersister();
            var c = new ChaosMonkeyConnectionAdapter(connection, successfulCalls);
            var context = new ContextBag();
            context.Set(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()));
            var session = new EventStoreSynchronizedStorageSession(c);
            var data = new SagaData();
            try
            {
                data = sagaId.HasValue
                    ? await persister.Get<SagaData>(sagaId.Value, session, context).ConfigureAwait(false)
                    : await persister.Get<SagaData>("StringProperty", correlationProperty, session, context).ConfigureAwait(false);

                sagaId = data?.Id ?? Guid.NewGuid();

                outgoingMessages.Add(sagaId.Value);

                if (data == null)
                {
                    data = new SagaData()
                    {
                        Id = sagaId.Value,
                        OriginalMessageId = messageId,
                        StringProperty = correlationProperty
                    };
                    await persister.Save(data, new SagaCorrelationProperty("StringProperty", correlationProperty), session, context).ConfigureAwait(false);
                }
                else
                {
                    await persister.Update(data, session, context).ConfigureAwait(false);
                }
            }
            catch (ChaosException)
            {
            }
            return data?.Id;
        }

        
        class SagaData : ContainSagaData
        {
            public string StringProperty { get; set; }
        }

        [SetUp]
        public void Connect()
        {
            connection = EventStoreConnection.Create(new IPEndPoint(IPAddress.Loopback, 1113));
            connection.ConnectAsync().GetAwaiter().GetResult();
        }

        [TearDown]
        public void Disconnect()
        {
            connection.Dispose();
        }
    }
}