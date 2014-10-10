namespace NServiceBus.Persistence.EventStore.TimeoutPersister.Events
{
    class Start
    {
        public long Epoch { get; set; }

        public Start(long epoch)
        {
            Epoch = epoch;
        }

        public Start()
        {
        }
    }
}