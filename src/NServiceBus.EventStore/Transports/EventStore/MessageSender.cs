using System.Transactions;
using EventStore.ClientAPI;
using NServiceBus.Unicast;

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

        public void Send(TransportMessage message, SendOptions sendOptions)
        {
            var destination = sendOptions.Destination;
            if (eventSourcedUnitOfWork.IsInitialized)
            {
                eventSourcedUnitOfWork.Publish(message.ToIndirectCommandEventData(destination));
            }
            else if (Transaction.Current != null)
            {
                transactionalUnitOfWork.Send(message.ToIndirectCommandEventData(destination));
            }
            else
            {
                connectionManager.GetConnection().AppendToStreamAsync(EndpointAddress.GetOutgoingStream(), ExpectedVersion.Any, eventData).Wait();
                             .AppendToStreamAsync(destination.GetDirectInputStream(), ExpectedVersion.Any, message.ToDirectCommandEventData(destination))
            }
        }
    }
}