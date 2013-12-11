namespace NServiceBus.Transports.EventStore.EventSourced
{
    public class EventSourcedMessageSender : ISendMessages
    {
        private readonly EventSourcedUnitOfWork unitOfWork;

        public EventSourcedMessageSender(EventSourcedUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public void Send(TransportMessage message, Address address)
        {
            unitOfWork.Enqueue(message.ToIndirectCommandEventData(address));
        }
    }
}