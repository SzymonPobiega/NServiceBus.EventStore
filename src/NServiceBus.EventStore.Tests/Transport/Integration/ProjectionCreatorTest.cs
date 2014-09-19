using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Projections;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class ProjectionCreatorTest<T> : TransportIntegrationTest
        where T : AbstractProjectionCreator, new()
    {
        protected abstract string ProjectionName { get; }

        [Test]
        public void Can_create_projection()
        {
            //Arrange
            var creator = new T
                {
                    ConnectionManager = new DefaultConnectionManager(ConnectionConfiguration)
                };
            var projectionManager = new DefaultProjectionsManager(new ProjectionsManager(new NoopLogger(), HttpEndPoint, TimeSpan.FromSeconds(90)), AdminCredentials);

            //Act
            creator.RegisterProjectionsFor(new Address("comp1", "store1"), "account");

            //Assert
            var projection = projectionManager.GetStatus(ProjectionName);
            Assert.AreEqual(ManagedProjectionState.Running, projection.StatusEnum);
        }
    }
}