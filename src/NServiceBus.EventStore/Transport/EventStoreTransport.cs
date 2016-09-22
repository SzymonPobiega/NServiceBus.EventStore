using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus
{
    /// <summary>
    /// NServiceBus transport using EventStore.
    /// </summary>
    public class EventStoreTransport : TransportDefinition
    {
        public override Transport.TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new TransportInfrastructure(settings, connectionString);
        }

        public override string ExampleConnectionStringForErrorMessage => "singleNode=127.0.0.1";
    }
}