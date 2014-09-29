using System;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class SagaIndexEvent
    {
        public Guid SagaId { get; set; }
        public string OriginalMessageId { get; set; }

        public SagaIndexEvent(Guid sagaId, string originalMessageId)
        {
            SagaId = sagaId;
            OriginalMessageId = originalMessageId;
        }

        public SagaIndexEvent()
        {
        }
    }
}