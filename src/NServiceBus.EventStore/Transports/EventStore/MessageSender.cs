using System.Transactions;
using EventStore.ClientAPI;

namespace NServiceBus.Transports.EventStore
{
    public class MessageSender : ISendMessages
    {
        public Address EndpointAddress { get; set; }

        private readonly TransactionalUnitOfWork transactionalUnitOfWork;
        private readonly IEventSourcedUnitOfWork eventSourcedUnitOfWork;
        private readonly IManageEventStoreConnections connectionManager;

        public MessageSender(TransactionalUnitOfWork transactionalUnitOfWork, IEventSourcedUnitOfWork eventSourcedUnitOfWork, IManageEventStoreConnections connectionManager)
        {
            this.transactionalUnitOfWork = transactionalUnitOfWork;
            this.connectionManager = connectionManager;
            this.eventSourcedUnitOfWork = eventSourcedUnitOfWork;
        }

        public void Send(TransportMessage message, Address address)
        {
            var eventData = message.ToIndirectCommandEventData(address);
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