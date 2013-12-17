using NServiceBus.Transports;

namespace NServiceBus
{
    public class EventStore : TransportDefinition
    {
        public EventStore()
        {
            HasNativePubSubSupport = true;
            HasSupportForCentralizedPubSub = false;
        }
    }
}