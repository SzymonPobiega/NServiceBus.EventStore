using NServiceBus.Persistence.EventStore.SagaPersister;

namespace NServiceBus
{
    class SagaIndexEntry
    {
        public SagaIndexEvent IndexEvent { get; }
        public byte[] SagaData { get; }

        public SagaIndexEntry(SagaIndexEvent indexEvent, byte[] sagaData)
        {
            IndexEvent = indexEvent;
            SagaData = sagaData;
        }
    }
}