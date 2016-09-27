using System;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class SagaMarkerEvent
    {
        public Guid SagaId { get; set; }
        public string SagaIndexStream { get; set; }

        public SagaMarkerEvent()
        {
        }
        public SagaMarkerEvent(Guid sagaId, string sagaIndexStream)
        {
            SagaId = sagaId;
            SagaIndexStream = sagaIndexStream;
        }
    }
}