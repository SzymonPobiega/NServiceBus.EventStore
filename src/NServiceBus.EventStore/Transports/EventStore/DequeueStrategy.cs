using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Transports.EventStore
{
    public class DequeueStrategy : IDequeueMessages
    {
        private readonly IManageEventStoreConnections connectionManager;

        public DequeueStrategy(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public void Init(Address address, 
            TransactionSettings transactionSettings, 
            Func<TransportMessage, bool> tryProcessMessage, 
            Action<TransportMessage, Exception> endProcessMessage)
        {
            this.tryProcessMessage = tryProcessMessage;
            this.endProcessMessage = endProcessMessage;
            endpointAddress = address;
        }

        public void Start(int maximumConcurrencyLevel)
        {
            subscriptions = Enumerable.Range(0, maximumConcurrencyLevel)
                .Select(x => connectionManager.GetConnection().ConnectToPersistentSubscription(GetSubscriptionId(), endpointAddress.GetFinalIncomingQueue(), true, OnEvent))
                .ToList();
        }

        private string GetSubscriptionId()
        {
            return endpointAddress.Queue;
        }

        private void OnEvent(EventStorePersistentSubscription subscription, ResolvedEvent evnt)
        {
            var transportMessage = evnt.ToTransportMessage();
            if (transportMessage == null) //system message
            {
                return;
            }
            while (true) //First-level retry loop
            {
                Exception processingError = null;
                try
                {
                    if (tryProcessMessage(transportMessage))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    processingError = ex;
                }
                finally
                {
                    endProcessMessage(transportMessage, processingError);
                }   
            }            
        }

        public void Stop()
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Stop(TimeSpan.FromSeconds(60));
            }
        }

        private Address endpointAddress;
        private List<EventStorePersistentSubscription> subscriptions;
        private Func<TransportMessage, bool> tryProcessMessage;
        private Action<TransportMessage, Exception> endProcessMessage;
    }
}