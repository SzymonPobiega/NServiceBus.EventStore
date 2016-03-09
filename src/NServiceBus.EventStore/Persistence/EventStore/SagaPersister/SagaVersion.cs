namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class SagaVersion
    {
        private readonly int version;

        public SagaVersion(int version)
        {
            this.version = version;
        }

        public int Version
        {
            get { return version; }
        }
    }
}