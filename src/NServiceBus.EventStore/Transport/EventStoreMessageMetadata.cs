using System;
using System.Collections.Generic;

namespace NServiceBus
{
    class EventStoreMessageMetadata
    {
        public string DestinationQueue { get; set; }
        public string MessageId { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool Empty { get; set; }
        public DateTime? TimeToBeReceived { get; set; }
    }
}