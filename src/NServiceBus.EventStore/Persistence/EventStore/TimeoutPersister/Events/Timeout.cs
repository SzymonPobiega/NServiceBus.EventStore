using System;
using NServiceBus.Timeout.Core;

namespace NServiceBus.Persistence.EventStore.TimeoutPersister.Events
{
    class Timeout
    {
        public long Epoch { get; set; }
        public DateTime DueTime { get; set; }
        public TimeoutData Data { get; set; }

        public Timeout(long epoch, DateTime dueTime, TimeoutData data)
        {
            Epoch = epoch;
            DueTime = dueTime;
            Data = data;
        }

        public Timeout()
        {
        }
    }
}