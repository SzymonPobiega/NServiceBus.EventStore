using EventStore.ClientAPI;

namespace NServiceBus
{
    class OutboxEventStoreSynchronizedStorageSession : EventStoreSynchronizedStorageSession
    {
        public EventData AddPersistenceOperation(string destinationStream, EventData eventData)
        {
            return outboxTransaction.AddPersistenceOperation(destinationStream, eventData);
        }

        public OutboxEventStoreSynchronizedStorageSession(IEventStoreConnection connection, EventStoreOutboxTransaction outboxTransaction)
            : base(connection)
        {
            this.outboxTransaction = outboxTransaction;
        }

        public override bool SupportsAtomicAppend => true;

        EventStoreOutboxTransaction outboxTransaction;
    }
}