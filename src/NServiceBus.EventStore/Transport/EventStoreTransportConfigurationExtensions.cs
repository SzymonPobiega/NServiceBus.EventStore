using NServiceBus.Configuration.AdvanceExtensibility;

namespace NServiceBus
{
    public static class EventStoreTransportConfigurationExtensions
    {
        /// <summary>
        /// Disables usage of cached exchange collections. Forces reading the exchange collection each time an event is published.
        /// </summary>
        public static TransportExtensions<EventStoreTransport> DisableExchangeCaching(this TransportExtensions<EventStoreTransport> transport)
        {
            transport.GetSettings().Set("NServiceBus.EventStore.DisableExchangeCaching", true);
            return transport;
        } 
    }
}