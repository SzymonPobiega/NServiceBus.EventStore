using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;
using NServiceBus.Internal;
using NServiceBus.Transports;

namespace NServiceBus
{
    public class EventStoreTransport : TransportDefinition
    {
        public EventStoreTransport()
        {
            HasNativePubSubSupport = true;
            HasSupportForCentralizedPubSub = true;
            HasSupportForDistributedTransactions = false;
        }

        protected override void Configure(BusConfiguration config)
        {
            config.UseSerialization<JsonSerializer>().Encoding(Json.UTF8NoBom);
            config.EnableFeature<EventStoreConnectionManager>();
            config.EnableFeature<EventStoreTransportFeature>();
            config.EnableFeature<TimeoutManagerBasedDeferral>();

            config.GetSettings().EnableFeatureByDefault<TimeoutManager>();

            //enable the outbox unless the users hasn't disabled it
            //if (config.GetSettings().GetOrDefault<bool>(typeof(Features.Outbox).FullName))
            //{
            //    config.EnableOutbox();
            //}
        }
    }
}