using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using NServiceBus.Transports;

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
            foreach (var creator in queueCreators)
            {
                creator.RegisterProjectionsFor(account);
            }
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
                if (inner == null || inner.Message != string.Format("Subscription group {0} on stream {0} alreay exists", groupName))
                {
                    throw;
                }
            }
        }
    }
}