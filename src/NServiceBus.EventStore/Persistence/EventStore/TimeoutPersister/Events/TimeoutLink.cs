using System;

namespace NServiceBus.Persistence.EventStore.TimeoutPersister.Events
{
    class TimeoutLink
    {
        public DateTime DueTime { get; set; }
        public string Link { get; set; }
        public long Epoch { get; set; }
        public bool EpochEnd { get; set; }

        public TimeoutLink(DateTime dueTime, string link, long epoch, bool epochEnd)
        {
            DueTime = dueTime;
            Link = link;
            Epoch = epoch;
            EpochEnd = epochEnd;
        }

        public TimeoutLink()
        {
        }
    }
}