using EventStore.ClientAPI;
using NServiceBus.Internal.Projections;

namespace NServiceBus.Internal
{
    public interface IManageEventStoreConnections
    {
        IEventStoreConnection GetConnection();
        IProjectionsManager GetProjectionManager();
    }
}