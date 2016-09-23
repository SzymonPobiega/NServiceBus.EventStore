using System;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class SagaIndexEvent
    {
        public string DataStreamName { get; set; }

        public SagaIndexEvent(string dataStreamName)
        {
            DataStreamName = dataStreamName;
        }

        public SagaIndexEvent()
        {
        }
    }
}