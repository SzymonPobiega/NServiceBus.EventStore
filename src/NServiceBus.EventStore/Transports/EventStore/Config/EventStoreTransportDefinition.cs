using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore.Serializers.Json;

namespace NServiceBus
{
    public class EventStoreTransportDefinition : TransportDefinition
    {
        public EventStoreTransportDefinition()
        {
            HasNativePubSubSupport = true;
            HasSupportForCentralizedPubSub = true;
            HasSupportForDistributedTransactions = false;
        }

        protected override void Configure(BusConfiguration config)
        {
            config.EnableFeature<EventStoreTransportFeature>();
            config.EnableFeature<TimeoutManagerBasedDeferral>();
            config.UseSerialization<JsonSerializer>().Encoding(JsonNoBomMessageSerializer.UTF8NoBom);

            config.GetSettings().EnableFeatureByDefault<TimeoutManager>();

            //enable the outbox unless the users hasn't disabled it
            if (config.GetSettings().GetOrDefault<bool>(typeof(Features.Outbox).FullName))
            {
                config.EnableOutbox();
            }
        }
    }
}