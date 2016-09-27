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

        /// <summary>
        /// Sets the ID for timeout processor.
        /// </summary>
        /// <param name="transport">ETransport.</param>
        /// <param name="id">The id.</param>
        public static TransportExtensions<EventStoreTransport> TimeoutProcessorId(this TransportExtensions<EventStoreTransport> transport, string id)
        {
            transport.GetSettings().Set("NServiceBus.EventStore.TimeoutProcessorId", id);
            return transport;
        }
    }
}