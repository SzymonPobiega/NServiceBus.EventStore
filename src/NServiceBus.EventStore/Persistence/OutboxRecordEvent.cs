using System;
using System.Collections.Generic;
using NServiceBus.Outbox;

namespace NServiceBus
{
    class OutboxRecordEvent
    {
        public TransportOperation[] TransportOperations { get; set; }
        public Dictionary<Guid, string> PersistenceOperations { get; set; } 
    }
}