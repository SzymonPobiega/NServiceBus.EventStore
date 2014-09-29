using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventStore.ClientAPI.Exceptions;
using NServiceBus.Persistence.EventStore.SagaPersister;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.SagaPersistence.Integration
{
    [TestFixture]
    public class SagaPersisterTests : IntegrationTest
    {
        public class TestMessage
        {
            public string DummyProperty { get; set; }
        }

        public class TestMessage2
        {
            public string DummyProperty { get; set; }
        }

        [SetUp]
        public void SetUpIndexing()
        {
            Features.Sagas.SagaEntityToMessageToPropertyLookup[typeof (TestSaga)] =
                new Dictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>()
                {
                    {typeof(TestMessage),new KeyValuePair<PropertyInfo, PropertyInfo>(typeof(TestSaga).GetProperty("StringField"),typeof(TestMessage).GetProperty("DummyProperty"))},
                    {typeof(TestMessage2),new KeyValuePair<PropertyInfo, PropertyInfo>(typeof(TestSaga).GetProperty("GuidField"),typeof(TestMessage2).GetProperty("DummyProperty"))},
                };
        }

        [Test]
        public void Can_store_and_load_saga_by_id()
        {
            var persister = new EventStoreSagaPersister(new DefaultConnectionManager(ConnectionConfiguration));

            var saga = new TestSaga
                {
                    Id = Guid.NewGuid(),
                    GuidField = Guid.NewGuid(),
                    StringField = "SomeString",
                    IntField = 77,
                    DateField = DateTime.UtcNow
                };

            persister.Save(saga);

            var loadedSaga = persister.Get<TestSaga>(saga.Id);

            Assert.AreEqual(saga.StringField, loadedSaga.StringField);
            Assert.AreEqual(saga.IntField, loadedSaga.IntField);
            Assert.AreEqual(saga.GuidField, loadedSaga.GuidField);
            Assert.AreEqual(saga.DateField, loadedSaga.DateField);
        }

        [Test]
        public void Can_load_saga_by_string_field()
        {
            var persister = new EventStoreSagaPersister(new DefaultConnectionManager(ConnectionConfiguration));

            var saga = new TestSaga
            {
                Id = Guid.NewGuid(),
                GuidField = Guid.NewGuid(),
                StringField = "SomeString",
                IntField = 77,
                DateField = DateTime.UtcNow
            };

            persister.Save(saga);

            var loadedSaga = persister.Get<TestSaga>("StringField", "SomeString");

            Assert.IsNotNull(loadedSaga);
            Assert.AreEqual(saga.StringField, loadedSaga.StringField);
        }

        [Test]
        public void Can_update_saga()
        {
            var persister = new EventStoreSagaPersister(new DefaultConnectionManager(ConnectionConfiguration));

            var saga = new TestSaga
            {
                Id = Guid.NewGuid(),
                GuidField = Guid.NewGuid(),
                StringField = "SomeString",
                IntField = 77,
                DateField = DateTime.UtcNow
            };

            persister.Save(saga);

            var loadedSaga = persister.Get<TestSaga>(saga.Id);

            loadedSaga.StringField = "SomeOtherString";
            loadedSaga.IntField = 66;

            persister.Update(loadedSaga);

            var loadedAgainSaga = persister.Get<TestSaga>(saga.Id);

            Assert.AreEqual(loadedSaga.StringField, loadedAgainSaga.StringField);
            Assert.AreEqual(loadedSaga.IntField, loadedAgainSaga.IntField);
        }

        [Test]
        public void After_failure_saving_saga_a_failed_attempt_can_be_retried_by_handler_of_same_message()
        {
            var faultyPersister = new EventStoreSagaPersister(new FaultyConnectionManager(new DefaultConnectionManager(ConnectionConfiguration),2));

            var saga = new TestSaga
            {
                OriginalMessageId = "123",
                Id = Guid.NewGuid(),
                GuidField = Guid.NewGuid(),
                StringField = "SomeString",
                IntField = 77,
                DateField = DateTime.UtcNow
            };

            try
            {
                faultyPersister.Save(saga);
                Assert.Fail("Expecting failure");
            }
            catch (Exception) //Index stored, saga not stored
            {
            }
            faultyPersister.Save(saga);
        }

        [Test]
        public void After_failure_saving_saga_a_failed_attempt_cannot_be_retried_by_handler_of_different_message()
        {
            var persister = new EventStoreSagaPersister(new FaultyConnectionManager(new DefaultConnectionManager(ConnectionConfiguration), 2));

            var saga = new TestSaga
            {
                OriginalMessageId = "123",
                Id = Guid.NewGuid(),
                GuidField = Guid.NewGuid(),
                StringField = "SomeString",
                IntField = 77,
                DateField = DateTime.UtcNow
            };

            try
            {
                persister.Save(saga);
                Assert.Fail("Expecting failure");
            }
            catch (Exception) //Index stored, saga not stored
            {
            }

            saga.OriginalMessageId = "456";
            var exception = Assert.Throws<System.AggregateException>(() => persister.Save(saga));
            var inner = exception.InnerExceptions.First();
            Assert.IsInstanceOf<WrongExpectedVersionException>(inner);
        }
        
        [Test]
        public void Detects_concurrent_writes_to_existing_sagas()
        {
            var persister = new EventStoreSagaPersister(new DefaultConnectionManager(ConnectionConfiguration));

            var saga = new TestSaga
            {
                Id = Guid.NewGuid(),
                GuidField = Guid.NewGuid(),
                StringField = "SomeString",
                IntField = 77,
                DateField = DateTime.UtcNow
            };

            persister.Save(saga);

            var loadedSaga = persister.Get<TestSaga>(saga.Id);

            loadedSaga.StringField = "SomeOtherString";
            persister.Update(loadedSaga);

            loadedSaga.StringField = "InvalidVersionUpdated";
            var exception = Assert.Throws<System.AggregateException>(() => persister.Update(loadedSaga));
            var inner = exception.InnerExceptions.First();
            Assert.IsInstanceOf<WrongExpectedVersionException>(inner);
        }
    }
}