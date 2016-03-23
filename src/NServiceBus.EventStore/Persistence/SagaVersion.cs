namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class SagaVersion
    {
        public bool? AlreadyLocked { get; }
        public int Version { get; }

        public SagaVersion(int version, bool? alreadyLocked)
        {
            this.Version = version;
            this.AlreadyLocked = alreadyLocked;
        }

    }
}