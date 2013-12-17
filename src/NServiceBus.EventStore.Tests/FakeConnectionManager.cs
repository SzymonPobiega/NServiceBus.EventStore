using EventStore.ClientAPI;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Projections;

namespace NServiceBus.AddIn.Tests
{
    public class FakeConnectionManager : IManageEventStoreConnections
    {
        public FakeProjectionsManager ProjectionsManager { get; set; }
        public IEventStoreConnection Connection { get; set; }

        public FakeConnectionManager()
        {
            ProjectionsManager = new FakeProjectionsManager();
        }

        public IEventStoreConnection GetConnection()
        {
            return Connection;
        }

        public IProjectionsManager GetProjectionManager()
        {
            return ProjectionsManager;
        }
    }
}