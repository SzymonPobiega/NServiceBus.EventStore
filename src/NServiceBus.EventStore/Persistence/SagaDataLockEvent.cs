using System;

namespace NServiceBus
{
    class SagaDataLockEvent
    {
        public string LockedBy { get; set; }

        public SagaDataLockEvent()
        {
        }

        public SagaDataLockEvent(string lockedBy)
        {
            LockedBy = lockedBy;
        }
    }
}