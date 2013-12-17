using System;
using System.Collections.Generic;
using NServiceBus.Timeout.Core;

namespace NServiceBus.Persistence.EventStore.TimeoutPersister
{
    public class EventStoreTimeoutPersister : IPersistTimeouts
    {
        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            throw new NotImplementedException();
        }

        public void Add(TimeoutData timeout)
        {
            throw new NotImplementedException();
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            throw new NotImplementedException();
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            throw new NotImplementedException();
        }
    }
}