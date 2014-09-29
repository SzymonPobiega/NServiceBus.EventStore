using System.Collections.Generic;

namespace NServiceBus.Transports.EventStore
{
    public class EventStoreMessageMetadata
    {
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public string ReplyTo { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string DestinationQueue { get; set; }
    }
}