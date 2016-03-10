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
        private readonly IManageEventStoreConnections connectionManager;

        public EventStoreQueueCreator(IEnumerable<IRegisterProjections> queueCreators, IManageEventStoreConnections connectionManager)
        {
            this.queueCreators = queueCreators;
            this.connectionManager = connectionManager;
        }

        private void EnsureSubscriptionExists(string queue)
        {
            try
            {
                var result = connectionManager.GetConnection()
                    .CreatePersistentSubscriptionAsync(queue, queue,
                        PersistentSubscriptionSettingsBuilder.Create(), null)
                    .Result;
            }
            catch (AggregateException ex)
            {
                var inner = ex.InnerException as InvalidOperationException;
                if (inner == null ||
                    inner.Message != string.Format("Subscription group {0} on stream {0} alreay exists", queue))
                {
                    throw;
                }
            }
        }

        private void RegisterProjections(string account)
        {
            foreach (var creator in queueCreators)
            {
                creator.RegisterProjectionsFor(account);
            }
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            RegisterProjections(identity);
            foreach (var queueBinding in queueBindings.ReceivingAddresses)
            {
                EnsureSubscriptionExists(queueBinding);
            }
            return Task.FromResult(0);
        }
    }
}