namespace NServiceBus.EventSourcing
{
    class AggregateLockEvent
    {
        public string LockedBy { get; set; }

        public AggregateLockEvent()
        {
        }

        public AggregateLockEvent(string lockedBy)
        {
            LockedBy = lockedBy;
        }
    }
}