using NServiceBus.Transports.EventStore.Config;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public static class ConfigureEventStoreSagaPersister
    {
        public static Configure EventStoreSagaPersister(this Configure config)
        {
            if (!Configure.HasComponent<IConnectionConfiguration>())
            {
                config.EventStore();
            }
            config.Configurer.ConfigureComponent<EventStoreSagaPersister>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<EventStoreSagaIndexer>(DependencyLifecycle.InstancePerCall);
            return config;
        }
    }
}