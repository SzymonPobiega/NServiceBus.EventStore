using EventStore.ClientAPI;
using NServiceBus.Transports.EventStore.Projections;

namespace NServiceBus.Transports.EventStore
{
    public interface IManageEventStoreConnections
    {
        IEventStoreConnection GetConnection();
        IProjectionsManager GetProjectionManager();
    }
}