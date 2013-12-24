using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Persistence.EventStore.SagaPersister;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.SagaPersistence.Integration
{
    [TestFixture]
    public class SagaIndexerTests : IntegrationTest
    {
        private EventStoreSagaPersister persister;

        [Test]
        public void Can_find_saga_by_string_property()
        {            
            var loadedSaga = persister.Get<TestSaga>("StringField", "SomeString");
            Assert.NotNull(loadedSaga);
        }
        
        [Test]
        public void Can_find_saga_by_int_property()
        {            
            var loadedSaga = persister.Get<TestSaga>("IntField", "77");
            Assert.NotNull(loadedSaga);
        }
        
        [Test]
        public void Can_find_saga_by_guid_property()
        {
            var loadedSaga = persister.Get<TestSaga>("GuidField", "ee63895f-685b-4f1a-b7ef-2a7b0dcffcb5");
            Assert.NotNull(loadedSaga);
        }

        [SetUp]
        public void Setup()
        {
            var projectionsManager = new ProjectionsManager(new NoopLogger(), HttpEndPoint);
            projectionsManager.Enable("$by_category", AdminCredentials);

            var projectionCreator = new SagaIndexerProjectionCreator
                {
                    ConnectionManager = new DefaultConnectionManager(ConnectionConfiguration)
                };
            projectionCreator.RegisterProjectionsFor(null, null);
            
            persister = new EventStoreSagaPersister(new DefaultConnectionManager(ConnectionConfiguration));

            var saga = new TestSaga
                {
                    Id = Guid.NewGuid(),
                    GuidField = new Guid("EE63895F-685B-4F1A-B7EF-2A7B0DCFFCB5"),
                    StringField = "SomeString",
                    IntField = 77,
                    DateField = DateTime.UtcNow
                };

            persister.Save(saga);

            var indexer = new EventStoreSagaIndexer(new DefaultConnectionManager(ConnectionConfiguration));
            var messageMap = new Dictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>
                {
                    {
                        typeof (Message1),
                        new KeyValuePair<PropertyInfo, PropertyInfo>(typeof (TestSaga).GetProperty("StringField"), null)
                    },
                    {
                        typeof (Message2),
                        new KeyValuePair<PropertyInfo, PropertyInfo>(typeof (TestSaga).GetProperty("IntField"), null)
                    },
                    {
                        typeof (Message3),
                        new KeyValuePair<PropertyInfo, PropertyInfo>(typeof (TestSaga).GetProperty("GuidField"), null)
                    }
                };

            var sagaEntityToMessageToPropertyLookup =
                new Dictionary<Type, IDictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>>()
                    {
                        {
                            typeof (TestSaga), messageMap
                        }
                    };
            indexer.EnsureIndicesForProperties(sagaEntityToMessageToPropertyLookup);

            Thread.Sleep(1000);
        }

        public class Message1
        {
        }
        public class Message2
        {
        }
        public class Message3
        {
        }
    }
}