using System;
using System.Collections.Generic;
using System.Transactions;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Unicast;

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

        public void Publish(TransportMessage message, PublishOptions publishOptions)
        {
            var eventData = message.ToEventEventData(publishOptions.ReplyToAddress);

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
                connectionManager.GetConnection().AppendToStreamAsync(EndpointAddress.GetOutgoingStream(), ExpectedVersion.Any, eventData).Wait();
            }
        }
    }
}