using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Extensibility;
using NServiceBus.Internal;

namespace NServiceBus.Transports.EventStore
{
    class SubscriptionManager : IManageSubscriptions
    {
        IConnectionConfiguration connectionConfig;
        string endpointName;

        public SubscriptionManager(IConnectionConfiguration connectionConfig, string endpointName)
        {
            this.connectionConfig = connectionConfig;
            this.endpointName = endpointName;
        }

        private async Task ChangeSubscription(string action, Type eventType)
        {
            var data = new SubscriptionEvent
            {
                SubscriberEndpoint = endpointName,
                EventType = eventType.AssemblyQualifiedName,
            };
            using (var connection = connectionConfig.CreateConnection())
            {
                await connection.ConnectAsync().ConfigureAwait(false);
                await connection.AppendToStreamAsync("events-subscriptions", ExpectedVersion.Any, new EventData(Guid.NewGuid(), action, true, data.ToJsonBytes(), new byte[0])).ConfigureAwait(false);
            }
        }

        public async Task Subscribe(Type eventType, ContextBag context)
        {
            await ChangeSubscription("$subscribe", eventType).ConfigureAwait(false);
        }

        public Task Unsubscribe(Type eventType, ContextBag context)
        {
            return ChangeSubscription("$unsubscribe", eventType);
        }
    }
}