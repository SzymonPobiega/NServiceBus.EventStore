using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Extensibility;
using NServiceBus.Internal;

namespace NServiceBus.Transports.EventStore
{
    class SubscriptionManager : IManageSubscriptions
    {
        IManageEventStoreConnections connectionManager;
        string endpointName;

        public SubscriptionManager(IManageEventStoreConnections connectionManager, string endpointName)
        {
            this.connectionManager = connectionManager;
            this.endpointName = endpointName;
        }

        private Task ChangeSubscription(string action, Type eventType)
        {
            var data = new SubscriptionEvent
            {
                SubscriberEndpoint = endpointName,
                EventType = eventType.AssemblyQualifiedName,
            };
            return connectionManager.GetConnection()
                .AppendToStreamAsync("events-subscriptions", ExpectedVersion.Any,
                    new EventData(Guid.NewGuid(), action, true, data.ToJsonBytes(), new byte[0]));
        }

        public Task Subscribe(Type eventType, ContextBag context)
        {
            return ChangeSubscription("$subscribe", eventType);
        }

        public Task Unsubscribe(Type eventType, ContextBag context)
        {
            return ChangeSubscription("$unsubscribe", eventType);
        }
    }
}