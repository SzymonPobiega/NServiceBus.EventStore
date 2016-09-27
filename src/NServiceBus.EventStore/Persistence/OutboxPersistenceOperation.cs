using EventStore.ClientAPI;

namespace NServiceBus
{
    class OutboxPersistenceOperation
    {
        public OutboxPersistenceOperation(string destinationStream, EventData @event)
        {
            DestinationStream = destinationStream;
            Event = @event;
        }

        public EventData Event { get; }
        public string DestinationStream { get; }
    }
}