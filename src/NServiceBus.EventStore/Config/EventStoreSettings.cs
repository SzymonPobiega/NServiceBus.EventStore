using System;
using NServiceBus.Settings;

namespace NServiceBus.Transports.EventStore.Config
{
    public class EventStoreSettings
    {
        public EventStoreSettings UseEventSourcing()
        {
            SettingsHolder.Set("NServiceBus.EventStore.EventSourcing",true);
            return this;
        }
    }

    public static class EventStoreSettingsExtensions
    {
        public static TransportSettings EventStore(this TransportSettings configuration, Action<EventStoreSettings> action)
        {
            action(new EventStoreSettings());
            return configuration;
        }
    }
}