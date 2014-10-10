namespace NServiceBus.Persistence.EventStore.TimeoutPersister.Events
{
    class Checkpoint
    {
        public long Epoch { get; set; }

        public Checkpoint(long epoch)
        {
            Epoch = epoch;
        }

        public Checkpoint()
        {
        }
    }
}