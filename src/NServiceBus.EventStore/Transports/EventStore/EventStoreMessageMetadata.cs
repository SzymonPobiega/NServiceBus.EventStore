using System.Collections.Generic;

namespace NServiceBus.Transports.EventStore
{
    public class EventStoreMessageMetadata
    {
        public string DestinationQueue { get; set; }
        public string MessageId { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}