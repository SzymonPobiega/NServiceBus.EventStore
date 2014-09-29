using System;
using EventStore.ClientAPI;
using NServiceBus.Transports.EventStore.Serializers.Json;

namespace NServiceBus.Transports.EventStore
{
    public class SubscriptionManager : IManageSubscriptions
    {
        public Address EndpointAddress { get; set; }

        private readonly IManageEventStoreConnections connectionManager;

        public SubscriptionManager(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public void Subscribe(Type eventType, Address publisherAddress)
        {
            ChangeSubscription("$subscribe", eventType);
        }

        private void ChangeSubscription(string action, Type eventType)
        {
            var data = new SubscriptionEvent()
            {
                SubscriberEndpoint = EndpointAddress.Queue,
                EventType = eventType.AssemblyQualifiedName,
            };
            connectionManager.GetConnection()
                .AppendToStreamAsync("events-subscriptions", ExpectedVersion.Any,
                    new EventData(Guid.NewGuid(), action, true, data.ToJsonBytes(), new byte[0]))
                .Wait();
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            ChangeSubscription("$unsubscribe", eventType);
        }
    }
}