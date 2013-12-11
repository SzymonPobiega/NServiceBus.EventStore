using System;
using System.Collections.Generic;
using System.Transactions;
using EventStore.ClientAPI;

namespace NServiceBus.Transports.EventStore.Transactional
{
    public class TransactionalMessagePublisher : IPublishMessages
    {
        public Address EndpointAddress { get; set; }

        private readonly TransactionalUnitOfWork unitOfWork;
        private readonly IManageEventStoreConnections connectionManager;

        public TransactionalMessagePublisher(TransactionalUnitOfWork unitOfWork, IManageEventStoreConnections connectionManager)
        {
            this.unitOfWork = unitOfWork;
            this.connectionManager = connectionManager;
        }

        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            if (Transaction.Current != null)
            {
                unitOfWork.Send(message.ToEventEventData(eventTypes));
            }
            else
            {
                connectionManager.GetConnection()
                             .AppendToStream(EndpointAddress.GetFinalOutgoingQueue(), ExpectedVersion.Any, message.ToEventEventData(eventTypes));
            }
            return true;
        }
    }
}