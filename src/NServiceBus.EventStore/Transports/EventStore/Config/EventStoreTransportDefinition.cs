using NServiceBus.Transports;

namespace NServiceBus
{
    public class EventStoreTransportDefinition : TransportDefinition
    {
        public EventStoreTransportDefinition()
        {
            HasNativePubSubSupport = true;
            HasSupportForCentralizedPubSub = true;
        }
    }
}