using System;
using System.Collections.Generic;
using System.Transactions;
using EventStore.ClientAPI;

namespace NServiceBus.Transports.EventStore
{
    public class MessagePublisher : IPublishMessages
    {
        public Address EndpointAddress { get; set; }

        private readonly TransactionalUnitOfWork transactionalUnitOfWork;
        private readonly IEventSourcedUnitOfWork eventSourcedUnitOfWork;
        private readonly IManageEventStoreConnections connectionManager;

        public MessagePublisher(TransactionalUnitOfWork transactionalUnitOfWork, 
            IEventSourcedUnitOfWork eventSourcedUnitOfWork, 
            IManageEventStoreConnections connectionManager)
        {
            this.transactionalUnitOfWork = transactionalUnitOfWork;
            this.connectionManager = connectionManager;
            this.eventSourcedUnitOfWork = eventSourcedUnitOfWork;
        }

        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            var eventData = message.ToEventEventData();

            if (eventSourcedUnitOfWork.IsInitialized)
            {
                eventSourcedUnitOfWork.Publish(eventData);
            }
            else if (Transaction.Current != null)
            {
                transactionalUnitOfWork.Send(eventData);
            }
            else
            {
                connectionManager.GetConnection().AppendToStreamAsync(EndpointAddress.OutgoingStream(), ExpectedVersion.Any, eventData).Wait();
            }
            return true;
        }
    }
}