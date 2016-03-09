using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Internal.Projections;

namespace NServiceBus.EventStore.Tests
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