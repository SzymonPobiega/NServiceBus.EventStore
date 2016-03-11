using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;

namespace NServiceBus
{
    class EventStoreQueueCreator : ICreateQueues
    {
        private readonly IEnumerable<IRegisterProjections> queueCreators;
        private readonly IConnectionConfiguration connectionConfig;

        public EventStoreQueueCreator(IEnumerable<IRegisterProjections> queueCreators, IConnectionConfiguration connectionConfig)
        {
            this.queueCreators = queueCreators;
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

        private void RegisterProjections()
        {
            var projectionsManager = connectionConfig.CreateProjectionsManager();
            foreach (var creator in queueCreators)
            {
                creator.RegisterProjectionsFor(projectionsManager);
            }
        }

        public async Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            RegisterProjections();

            using (var connection = connectionConfig.CreateConnection())
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