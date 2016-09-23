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

        public Task CreateIndex(string indexStream, string dataStream)
        {
            var indexPayload = new SagaIndexEvent(dataStream).ToJsonBytes();
            var indexEventData = new EventData(Guid.NewGuid(), SagaPersister.SagaIndexEventType, true, indexPayload, new byte[0]);
            return session.AppendToStreamAsync(indexStream, ExpectedVersion.NoStream, indexEventData);
        }

        public async Task<SagaIndexEntry> ReadIndex(string indexStream)
        {
            var slice = await session.ReadStreamEventsForwardAsync(indexStream, 0, 1, true).ConfigureAwait(false);
            if (slice.Status != SliceReadStatus.Success)
            {
                return null;
            }
            var indexEntry = slice.Events[0].Event.Data.ParseJson<SagaIndexEvent>();
            return new SagaIndexEntry(indexEntry.DataStreamName);
        }

        public TSagaData ReadSagaData<TSagaData>(string streamName, StreamEventsSlice readResult) where TSagaData : IContainSagaData
        {
            var lastEvent = readResult.Events[0];
            if (lastEvent.Link != null && !lastEvent.IsResolved)
            {
                //We got an invalid link. This means the previous message has failed committing the outbox transaction. We throw here to trigger retries.
                throw new Exception("A previous message destined for this saga has failed. Triggering retries.");
            }

            var saga = lastEvent.Event.Data.ParseJson<TSagaData>();
            var versionInfo = new SagaVersion(readResult.Events[0].OriginalEventNumber, streamName);
            session.StoreSagaVersion(saga.Id, versionInfo);
            return saga;
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