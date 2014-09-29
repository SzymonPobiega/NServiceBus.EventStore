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
            if (eventSourcedUnitOfWork.IsInitialized)
            {
                eventSourcedUnitOfWork.Publish(message.ToIndirectCommandEventData(address));
            }
            else if (Transaction.Current != null)
            {
                transactionalUnitOfWork.Send(message.ToIndirectCommandEventData(address));
            }
            else
            {
                connectionManager.GetConnection()
                             .AppendToStreamAsync(address.GetDirectInputStream(), ExpectedVersion.Any, message.ToDirectCommandEventData(address))
                             .Wait();
            }
        }
    }
}