using NServiceBus.Features;
using NServiceBus.Settings;
using NServiceBus.Transports;

namespace NServiceBus
{
    public class EventStoreTransport : TransportDefinition
    {
        protected override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new EventStoreTransportInfrastructure(settings, connectionString);
        }

        public override string ExampleConnectionStringForErrorMessage => "singleNode=127.0.0.1";
    }
}