using System;

namespace NServiceBus
{
    class SagaIndexEvent
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