using EventStore.ClientAPI;
using NServiceBus.Internal.Projections;

namespace NServiceBus.Internal
{
    public interface IConnectionConfiguration
    {
        IEventStoreConnection CreateConnection();
        IProjectionsManager CreateProjectionsManager();
    }
}