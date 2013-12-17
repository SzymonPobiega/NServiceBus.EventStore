using NServiceBus.Settings;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Config;
using NServiceBus.Unicast.Transport;

namespace NServiceBus
{
    public static class EventStoreConfigurator
    {
        public static Configure EventStore(this Configure config)
        {
            if (Configure.HasComponent<IConnectionConfiguration>())
            {
                return config;
            }
            var connectionString = TransportConnectionString.GetConnectionStringOrNull();
            var connectionConfiguration = new ConnectionStringParser().Parse(connectionString);

            Configure.Instance.Configurer.RegisterSingleton<IConnectionConfiguration>(connectionConfiguration);
            Configure.Component<DefaultConnectionManager>(DependencyLifecycle.SingleInstance);

            return config;
        }
    }
}
