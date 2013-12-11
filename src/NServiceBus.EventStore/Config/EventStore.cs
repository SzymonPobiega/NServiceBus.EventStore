namespace NServiceBus.Transports.EventStore.Config
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