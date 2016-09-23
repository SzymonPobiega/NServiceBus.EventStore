using NServiceBus.Persistence.EventStore.SagaPersister;

namespace NServiceBus
{
    class SagaIndexEntry
    {
        public string DataStreamName { get; }

        public SagaIndexEntry(string dataStreamName)
        {
            DataStreamName = dataStreamName;
        }
    }
}