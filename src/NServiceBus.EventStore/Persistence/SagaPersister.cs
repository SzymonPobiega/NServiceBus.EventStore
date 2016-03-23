using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Persistence;
using NServiceBus.Persistence.EventStore.SagaPersister;
using NServiceBus.Sagas;
using NServiceBus.Transports;

namespace NServiceBus
{
    /*

    Saga with outbox:
     Empty saga instance and index entry are created (with record of the lease time)
     Saga entity and version is added to the outbox record
     Before marking as dispatched, the saga instance data is copied from the outbox record to the saga instance     
    */
    class SagaPersister : ISagaPersister
    {
        const string SagaDataEventType = "$saga-data";
        const string SagaDataLockEventType = "$saga-data-lock";
        const string SagaIndexEventType = "$saga-index";

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            if (correlationProperty != null)
            {
                await CreateIndicesForSaga(sagaData, correlationProperty, session).ConfigureAwait(false);
            }
            await SaveData(sagaData, session).ConfigureAwait(false);
        }

        async Task CreateIndicesForSaga(IContainSagaData saga, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session)
        {
            var sagaType = saga.GetType();
            var propertyName = correlationProperty.Name;
            var propertyValue = correlationProperty.Value;
            try
            {
                await AddUniqueIndex(saga.Id, sagaType, saga.OriginalMessageId, propertyName, propertyValue, session).ConfigureAwait(false);
            }
            catch (WrongExpectedVersionException)
            {
                var indexEvent = await ReadIndex(sagaType, propertyName, propertyValue, session);
                if (indexEvent == null || saga.OriginalMessageId != indexEvent.OriginalMessageId)
                {
                    throw;
                }
            }
        }

        Task AddUniqueIndex(Guid sagaId, Type sagaType, string originalMessageId, string property, object value, SynchronizedStorageSession session)
        {
            var indexStream = BuildIndexStreamName(sagaType, property, value);
            var payload = new SagaIndexEvent(sagaId, originalMessageId).ToJsonBytes();
            var eventData = new EventData(Guid.NewGuid(), SagaIndexEventType, true, payload, new byte[0]);
            return session.AppendToStreamAsync(indexStream, ExpectedVersion.NoStream, eventData);
        }

        async Task<SagaIndexEvent> ReadIndex(Type sagaType, string property, object value, SynchronizedStorageSession session)
        {
            var indexStream = BuildIndexStreamName(sagaType, property, value);
            var slice = await session.ReadStreamEventsForwardAsync(indexStream, 0, 1, true).ConfigureAwait(false);
            if (slice.Status != SliceReadStatus.Success)
            {
                return null;
            }
            var indexEvent = slice.Events[0];
            var payload = indexEvent.Event.Data.ParseJson<SagaIndexEvent>();
            return payload;
        }

        static async Task SaveData(IContainSagaData saga, SynchronizedStorageSession session)
        {
            var streamName = BuildSagaStreamName(saga.GetType(), saga.Id);
            var eventData = new EventData(Guid.NewGuid(), SagaDataEventType, true, saga.ToJsonBytes(), new byte[0]);
            try
            {
                await session.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, eventData).ConfigureAwait(false);
            }
            catch (WrongExpectedVersionException)
            {
                //We can safely ignore it because the index entry has already been confirmed to come from same message.
            }
        }

        public async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var messageId = context.Get<IncomingMessage>().MessageId;
            var streamName = BuildSagaStreamName(sagaData.GetType(), sagaData.Id);

            var stateChangeEvent = new EventData(Guid.NewGuid(), SagaDataEventType, true, sagaData.ToJsonBytes(), new byte[0]);
            var lockEvent = new EventData(Guid.NewGuid(), SagaDataLockEventType, true, new SagaDataLockEvent(messageId).ToJsonBytes(), new byte[0]);

            var versionInfo = ((EventStoreSynchronizedStorageSession) session).GetSagaVersion(sagaData.Id);
            if (session.SupportsAtomicQueueForStore())
            {
                var alreadyLocked = versionInfo.AlreadyLocked ?? await CheckLock(messageId, streamName, sagaData, session).ConfigureAwait(false);
                if (alreadyLocked)
                {
                    await session.AppendToStreamAsync(streamName, versionInfo.Version, lockEvent).ConfigureAwait(false);
                }
                session.AtomicQueueForStore(streamName, stateChangeEvent);
            }
            else
            {
                await session.AppendToStreamAsync(streamName, versionInfo.Version, stateChangeEvent).ConfigureAwait(false);
            }
        }

        static async Task<bool> CheckLock(string messageId, string streamName, IContainSagaData sagaData, SynchronizedStorageSession session)
        {
            var readResult = await session.ReadStreamEventsBackwardAsync(streamName, -1, 1, false).ConfigureAwait(false);
            if (readResult.Status != SliceReadStatus.Success)
            {
                throw new Exception($"Cannot update saga {sagaData.Id} because it does not exist yet.");
            }
            if (readResult.Events[0].Event.EventType != SagaDataLockEventType)
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

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var messageId = context.Get<IncomingMessage>().MessageId;
            var streamName = BuildSagaStreamName(typeof(TSagaData), sagaId);
            return GetFromStream<TSagaData>(sagaId, messageId, streamName, session);
        }

        static async Task<TSagaData> GetFromStream<TSagaData>(Guid sagaId, string messageId, string streamName, SynchronizedStorageSession session) where TSagaData : IContainSagaData
        {
            var readResult = await session.ReadStreamEventsBackwardAsync(streamName, -1, 2, true).ConfigureAwait(false);
            if (readResult.Status != SliceReadStatus.Success)
            {
                return default(TSagaData);
            }
            var alreadyLocked = false;
            var stateEventIndex = 0;
            var lastEvent = readResult.Events[0].Event;
            if (lastEvent.EventType == SagaDataLockEventType)
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
            var versionInfo = new SagaVersion(lastEvent.EventNumber, alreadyLocked);
            ((EventStoreSynchronizedStorageSession)session).StoreSagaVersion(sagaId, versionInfo);
            return saga;
        }


        public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var index = await ReadIndex(typeof(TSagaData), propertyName, propertyValue, session);
            if (index == null)
            {
                return default(TSagaData);
            }
            var sagaData = await Get<TSagaData>(index.SagaId, session, context).ConfigureAwait(false);
            return sagaData;
        }

        string BuildIndexStreamName(Type sagaType, string property, object value)
        {
            return "nsb-saga-index-" + BuildSagaTypeName(sagaType) + "-by-" + property.ToLowerInvariant() + "#" + FormatValue(value);
        }

        string FormatValue(object value)
        {
            return value.ToString();
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var streamName = BuildSagaStreamName(sagaData.GetType(), sagaData.Id);
            return session.DeleteStreamAsync(streamName, ExpectedVersion.Any, true);
        }

        static string BuildSagaStreamName(Type sagaType, Guid sagaId)
        {
            return "nsb-saga-" + BuildSagaTypeName(sagaType) + "-" + sagaId.ToString("N");
        }

        static string BuildSagaTypeName(Type sagaType)
        {
            return sagaType.FullName.ToLowerInvariant().Replace(".", "-");
        }
    }
}