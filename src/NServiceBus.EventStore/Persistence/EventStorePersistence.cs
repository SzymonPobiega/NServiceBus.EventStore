using NServiceBus.Features;
using NServiceBus.Persistence;

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
            Supports<StorageType.Sagas>(s =>
            {
                s.EnableFeatureByDefault<EventStoreSynchronizedStorageFeature>();
                s.EnableFeatureByDefault<EventStoreSagaStorageFeature>();
            });
        }
    }
}