using NServiceBus.Features;
using NServiceBus.Settings;
using NServiceBus.Transports;

namespace NServiceBus
{
    /// <summary>
    /// NServiceBus transport using EventStore.
    /// </summary>
    public class EventStoreTransport : TransportDefinition
    {
        protected override Transports.TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new TransportInfrastructure(settings, connectionString);
        }

        public override string ExampleConnectionStringForErrorMessage => "singleNode=127.0.0.1";
    }
}