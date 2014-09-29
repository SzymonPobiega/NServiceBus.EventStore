using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using NServiceBus.Timeout.Core;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Serializers.Json;

namespace NServiceBus.Persistence.EventStore.TimeoutPersister
{
    public class EventStoreTimeoutPersister : IPersistTimeouts
    {
        private const string TimeoutDataEventType = "TimeoutData";
        private const string TimeoutProcessingCheckpointStream = "TimeoutProcessingCheckpoints";
        private readonly IManageEventStoreConnections connectionManager;
        public const int ResolutionInSeconds = 30;

        public EventStoreTimeoutPersister(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            throw new NotImplementedException();
        }

        public void Add(TimeoutData timeout)
        {
            var streamName = BuildTimeoutStreamName(timeout.SagaId);
            var meta = new TimeoutMetadata()
            {
                DueEpoch = (DateTime.UtcNow.Ticks - 621355968000000000L) / 10000000L
            };
            var eventData = new EventData(Guid.NewGuid(), TimeoutDataEventType, true, timeout.ToJsonBytes(), meta.ToJsonBytes());
            connectionManager.GetConnection().AppendToStreamAsync(streamName, ExpectedVersion.Any, eventData).Wait();
        }

        private string BuildTimeoutStreamName(Guid sagaId)
        {
            return "Timeout-" + sagaId.ToString("N");
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            throw new NotImplementedException();
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            connectionManager.GetConnection()
                .DeleteStreamAsync(BuildTimeoutStreamName(sagaId), ExpectedVersion.Any, true)
                .Wait();
        }
    }
}