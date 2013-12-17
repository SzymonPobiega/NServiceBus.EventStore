using System;
using System.Linq;
using EventStore.ClientAPI.Exceptions;
using NServiceBus.Persistence.EventStore.SagaPersister;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.SagaPersistence.Integration
{
    [TestFixture]
    public class SagaPersisterTests : IntegrationTest
    {
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
        public void Detects_concurrent_writes()
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