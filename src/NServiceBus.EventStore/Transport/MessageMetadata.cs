using System;
using System.Collections.Generic;

namespace NServiceBus
{
    class MessageMetadata
    {
        public string MessageId { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool Empty { get; set; }
        public DateTime? TimeToBeReceived { get; set; }
        public string Destination { get; set; }
    }
}