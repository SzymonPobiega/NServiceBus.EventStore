using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using NServiceBus.Internal;
using NServiceBus.Persistence;
using NServiceBus.Persistence.EventStore.SagaPersister;

namespace NServiceBus
{
    class SagaPersisterAtomicOperations
    {
        EventStoreSynchronizedStorageSession session;

        public SagaPersisterAtomicOperations(SynchronizedStorageSession session)
        {
            this.session = (EventStoreSynchronizedStorageSession)session;
        }

        public Task CreateMarker(string indexStream, IContainSagaData saga)
        {
            var markerPayload = new SagaMarkerEvent(saga.Id, indexStream).ToJsonBytes();
            var markerEventData = new EventData(Guid.NewGuid(), SagaPersister.SagaMarkerEventType, true, markerPayload, new byte[0]);
            return session.AppendToStreamAsync(BuildMarkerStreamName(saga.Id), ExpectedVersion.NoStream, markerEventData);
        }

        public async Task<SagaMarkerEvent> ReadMarker(Guid sagaId)
        {
            var markerReadResult = await session.ReadStreamEventsForwardAsync(BuildMarkerStreamName(sagaId), 0, 1, false).ConfigureAwait(false);
            return markerReadResult.Status == SliceReadStatus.Success
                ? markerReadResult.Events[0].Event.Data.ParseJson<SagaMarkerEvent>()
                : null;
        }

        public async Task DeleteMarker(IContainSagaData sagaData)
        {
            await session.DeleteStreamAsync(BuildMarkerStreamName(sagaData.Id), ExpectedVersion.Any, true).ConfigureAwait(false);
        }

        static string BuildMarkerStreamName(Guid sagaId)
        {
            return "nsb-saga-marker-" + sagaId.ToString("N");
        }

        public Task CreateIndex(string indexStream, IContainSagaData saga, string originalMessageId)
        {
            var indexPayload = new SagaIndexEvent(saga.Id, originalMessageId).ToJsonBytes();
            var indexEventData = new EventData(Guid.NewGuid(), SagaPersister.SagaIndexEventType, true, indexPayload, new byte[0]);
            var dataEventData = new EventData(Guid.NewGuid(), SagaPersister.SagaDataEventType, true, saga.ToJsonBytes(), new byte[0]);
            return session.AppendToStreamAsync(indexStream, ExpectedVersion.NoStream, indexEventData, dataEventData);
        }

        public async Task<SagaIndexEntry> ReadIndex(string indexStream)
        {
            var slice = await session.ReadStreamEventsForwardAsync(indexStream, 0, 2, true).ConfigureAwait(false);
            if (slice.Status != SliceReadStatus.Success)
            {
                return null;
            }
            var indexEntry = slice.Events[0].Event.Data.ParseJson<SagaIndexEvent>();
            return new SagaIndexEntry(indexEntry, slice.Events[1].Event.Data);
        }

        public Task SaveSagaData(string streamName, SagaVersion versionInfo, EventData stateChangeEvent)
        {
            return session.AppendToStreamAsync(streamName, versionInfo.Version, stateChangeEvent);
        }

        public async Task SaveSagaDataLink(string streamName, EventData linkEvent)
        {
            try
            {
                await session.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, linkEvent).ConfigureAwait(false);
            }
            catch (WrongExpectedVersionException)
            {
                //We can safely ignore it because the index entry has already been confirmed to come from same message.
            }
        }

        public async Task<TSagaData> ReadSagaData<TSagaData>(Guid sagaId, string messageId, string streamName) where TSagaData : IContainSagaData
        {
            var readResult = await session.ReadStreamEventsBackwardAsync(streamName, -1, 2, true).ConfigureAwait(false);
            if (readResult.Status != SliceReadStatus.Success)
            {
                return default(TSagaData);
            }
            var alreadyLocked = false;
            var stateEventIndex = 0;
            var lastEvent = readResult.Events[0].Event;
            if (lastEvent.EventType == SagaPersister.SagaDataLockEventType)
            {
                var lockEvent = lastEvent.Data.ParseJson<SagaDataLockEvent>();
                if (lockEvent.LockedBy != messageId)
                {
                    throw new Exception($"Saga instance {sagaId} has been locked for update by message {lockEvent.LockedBy}. This message has to be processed correctly first before other messages will be able to interact with this saga.");
                }
                alreadyLocked = true;
                stateEventIndex = 1;
            }
            var saga = readResult.Events[stateEventIndex].Event.Data.ParseJson<TSagaData>();
            var versionInfo = new SagaVersion(readResult.Events[0].OriginalEventNumber, alreadyLocked);
            session.StoreSagaVersion(sagaId, versionInfo);
            return saga;
        }

        public Task CreateLock(string messageId, string streamName, SagaVersion versionInfo)
        {
            var lockEvent = new EventData(Guid.NewGuid(), SagaPersister.SagaDataLockEventType, true, new SagaDataLockEvent(messageId).ToJsonBytes(), new byte[0]);
            return session.AppendToStreamAsync(streamName, versionInfo.Version, lockEvent);
        }

        public async Task<bool> CheckLock(string messageId, string streamName, IContainSagaData sagaData)
        {
            var readResult = await session.ReadStreamEventsBackwardAsync(streamName, -1, 1, false).ConfigureAwait(false);
            if (readResult.Status != SliceReadStatus.Success)
            {
                throw new Exception($"Cannot update saga {sagaData.Id} because it does not exist yet.");
            }
            if (readResult.Events[0].Event.EventType != SagaPersister.SagaDataLockEventType)
            {
                return false;
            }
            var existingLockEvent = readResult.Events[0].Event.Data.ParseJson<SagaDataLockEvent>();
            if (existingLockEvent.LockedBy != messageId)
            {
                throw new Exception($"Saga instance {sagaData.Id} has been locked for update by message {existingLockEvent.LockedBy}. This message has to be processed correctly first before other messages will be able to interact with this saga.");
            }
            return true;
        }

        public SagaVersion GetSagaVersion(Guid sagaId)
        {
            return session.GetSagaVersion(sagaId);
        }

        public void StoreSagaVersion(Guid sagaId, SagaVersion versionInfo)
        {
            session.StoreSagaVersion(sagaId, versionInfo);
        }
    }
}