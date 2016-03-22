using EventStore.ClientAPI;

namespace NServiceBus.Internal
{
    public interface IConnectionConfiguration
    {
        IEventStoreConnection CreateConnection(string type);
    }
}