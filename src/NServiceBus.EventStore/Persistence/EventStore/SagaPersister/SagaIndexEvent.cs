using System;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class SagaIndexEvent
    {
        public Guid SagaId { get; set; }

        public SagaIndexEvent(Guid sagaId)
        {
            SagaId = sagaId;
        }

        public SagaIndexEvent()
        {
        }
    }
}