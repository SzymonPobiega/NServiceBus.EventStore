using System.Transactions;
using EventStore.ClientAPI;

namespace NServiceBus.Transports.EventStore.Transactional
{
    public class TransactionalMessageSender : ISendMessages
    {
        public Address EndpointAddress { get; set; }

        private readonly TransactionalUnitOfWork unitOfWork;
        private readonly IManageEventStoreConnections connectionManager;

        public TransactionalMessageSender(TransactionalUnitOfWork unitOfWork, IManageEventStoreConnections connectionManager)
        {
            this.unitOfWork = unitOfWork;
            this.connectionManager = connectionManager;
        }

        public void Send(TransportMessage message, Address address)
        {
            if (Transaction.Current != null)
            {
                unitOfWork.Send(message.ToIndirectCommandEventData(address));
            }
            else
            {
                connectionManager.GetConnection()
                             .AppendToStream(address.GetReceiveAddressFrom(EndpointAddress), ExpectedVersion.Any, message.ToDirectCommandEventData(address));
            }
        }
    }
}