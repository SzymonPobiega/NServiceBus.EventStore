using EventStore.ClientAPI;
using NServiceBus.Transports.EventStore.Projections;

namespace NServiceBus.Transports.EventStore.Config
{
    public interface IConnectionConfiguration
    {
        IEventStoreConnection CreateConnection();
        IProjectionsManager CreateProjectionsManager();
    }
}