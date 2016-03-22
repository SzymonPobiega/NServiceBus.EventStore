using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Transports;

namespace NServiceBus
{
    class EventStoreQueueCreator : ICreateQueues
    {
        private readonly IConnectionConfiguration connectionConfig;

        public EventStoreQueueCreator(IConnectionConfiguration connectionConfig)
        {
            this.connectionConfig = connectionConfig;
        }

        private async Task EnsureSubscriptionExists(string queue, IEventStoreConnection connection)
        {
            try
            {
                await connection.CreatePersistentSubscriptionAsync(queue, queue, PersistentSubscriptionSettings.Create(), null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.Message != string.Format("Subscription group {0} on stream {0} already exists", queue))
                {
                    throw;
                }
            }
        }

        public async Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            using (var connection = connectionConfig.CreateConnection("CreateSubscription"))
            {
                await connection.ConnectAsync();
                foreach (var queueBinding in queueBindings.ReceivingAddresses)
                {
                    await EnsureSubscriptionExists(queueBinding, connection).ConfigureAwait(false);
                }
            }
        }
    }
}