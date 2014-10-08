using NServiceBus.Features;
using NServiceBus.Internal;
using NServiceBus.Persistence;
using NServiceBus.Persistence.EventStore.SagaPersister;

namespace NServiceBus
{
    /// <summary>
    /// Specifies the capabilities of the EventStore suite of storages
    /// </summary>
    public class EventStorePersistence : PersistenceDefinition
    {
        /// <summary>
        /// Defines the capabilities
        /// </summary>
        public EventStorePersistence()
        {
            Defaults(s => s.EnableFeatureByDefault<EventStoreConnectionManager>());

            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<InMemoryTimeoutPersistence>()); //For now
            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<EventStoreSagaStorage>());
        }
    }
}