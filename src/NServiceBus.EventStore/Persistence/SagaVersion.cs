namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    class SagaVersion
    {
        public int Version { get; }
        public string StreamName { get; }

        public SagaVersion(int version, string streamName)
        {
            Version = version;
            StreamName = streamName;
        }
    }
}