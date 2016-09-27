using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
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

        public async Task<TSagaData> ReadSagaData<TSagaData>(string streamName, ResolvedEvent lastEvent, string messageId)
           where TSagaData : IContainSagaData
        {
            TSagaData result;
            var count = 2;
            var currentEvent = lastEvent;
            while (!TryReadSagaData(streamName, currentEvent, messageId, out result))
            {
                var slice = await session.ReadStreamEventsBackwardAsync(streamName, -1, count, true).ConfigureAwait(false);
                if (slice.Status != SliceReadStatus.Success)
                {
                    return default(TSagaData);
                }
                currentEvent = slice.Events.Last();
                count++;
            }
            var versionInfo = new SagaVersion(lastEvent.OriginalEventNumber, streamName);
            session.StoreSagaVersion(result.Id, versionInfo);
            return result;
        }

        bool TryReadSagaData<TSagaData>(string streamName, ResolvedEvent lastEvent, string messageId, out TSagaData sagaData) 
            where TSagaData : IContainSagaData
        {
            if (lastEvent.Link != null && !lastEvent.IsResolved)
            {
                var linkMetadata = lastEvent.Link.Metadata.ParseJson<LinkEvent>();
                if (linkMetadata.MessageId != messageId)
                {
                    //We got an invalid link. This means the previous message has failed committing the outbox transaction. We throw here to trigger retries.
                    throw new Exception("A previous message destined for this saga has failed. Triggering retries.");
                }
                sagaData = default(TSagaData);
                return false;
            }

            sagaData = lastEvent.Event.Data.ParseJson<TSagaData>();            
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