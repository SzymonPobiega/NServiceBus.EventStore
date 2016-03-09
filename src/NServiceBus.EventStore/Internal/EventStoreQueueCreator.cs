using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;

namespace NServiceBus.Internal
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

        public void CreateQueueIfNecessary(Address address, string account)
        {
            var groupName = address.Queue;
            RegisterProjections(account);
            EnsureSubscriptionGroupExists(groupName);
        }

        private void EnsureSubscriptionGroupExists(string groupName)
        {
            try
            {
                var result = connectionManager.GetConnection()
                    .CreatePersistentSubscriptionAsync(groupName, groupName,
                        PersistentSubscriptionSettingsBuilder.Create(), null)
                    .Result;
            }
            catch (AggregateException ex)
            {
                var inner = ex.InnerException as InvalidOperationException;
                if (inner == null ||
                    inner.Message != string.Format("Subscription group {0} on stream {0} alreay exists", groupName))
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
    }
}