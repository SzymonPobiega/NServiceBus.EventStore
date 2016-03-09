namespace NServiceBus.Persistence.EventStore.TimeoutPersister.Events
{
    class Heartbeat
    {
        public long Epoch { get; set; }

        public Heartbeat(long epoch)
        {
            Epoch = epoch;
        }

        public Heartbeat()
        {
        }
    }
}