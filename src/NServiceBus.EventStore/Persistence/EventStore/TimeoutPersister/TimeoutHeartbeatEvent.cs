namespace NServiceBus.Persistence.EventStore.TimeoutPersister
{
    public class TimeoutHeartbeatEvent
    {
        public long Sequence { get; set; }
        public int Resolution { get; set; }
    }
}