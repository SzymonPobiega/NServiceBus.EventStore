using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using NServiceBus.Persistence.EventStore.SagaPersister;
using NServiceBus.Sagas;
using NServiceBus.Transports;

namespace NServiceBus
{
    class SagaPersister : ISagaPersister
    {
        const string SagaDataEventType = "$saga-data";
        const string SagaDataLockEventType = "$saga-data-lock";
        const string SagaIndexEventType = "$saga-index";
        const string SagaMarkerEventType = "$saga-marker";

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var streamName = BuildSagaStreamName(sagaData.GetType(), sagaData.Id);
            return correlationProperty != null
                ? SaveWithIndex(sagaData, correlationProperty, session, streamName)
                : SaveWithoutIndex(sagaData, session, streamName);
        }

        static async Task SaveWithoutIndex(IContainSagaData sagaData, SynchronizedStorageSession session, string streamName)
        {
            var eventData = new EventData(Guid.NewGuid(), SagaDataEventType, true, sagaData.ToJsonBytes(), new byte[0]);
            if (session.SupportsAtomicQueueForStore())
            {
                session.AtomicQueueForStore(streamName, eventData);
            }
            else
            {
                await session.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, eventData).ConfigureAwait(false);
            }
        }

        static async Task SaveWithIndex(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, string streamName)
        {
            var propertyName = correlationProperty.Name;
            var propertyValue = correlationProperty.Value;
            var indexStream = BuildIndexStreamName(sagaData.GetType(), propertyName, propertyValue);

            await AddMarker(indexStream, sagaData, session).ConfigureAwait(false);
            var linkEvent = await CreateIndex(sagaData, indexStream, session).ConfigureAwait(false);
            try
            {
                await session.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, linkEvent).ConfigureAwait(false);
            }
            catch (WrongExpectedVersionException)
            {
                //We can safely ignore it because the index entry has already been confirmed to come from same message.
            }
            await DeleteMarker(sagaData, session).ConfigureAwait(false);
        }

        static async Task DeleteMarker(IContainSagaData sagaData, SynchronizedStorageSession session)
        {
            await session.DeleteStreamAsync(BuildMarkerStreamName(sagaData.Id), ExpectedVersion.Any, true).ConfigureAwait(false);
        }

        static async Task<EventData> CreateIndex(IContainSagaData saga, string indexStream, SynchronizedStorageSession session)
        {
            try
            {
                await AddUniqueIndex(indexStream, saga, saga.OriginalMessageId, session).ConfigureAwait(false);
            }
            catch (WrongExpectedVersionException)
            {
                var indexEvent = await ReadIndex(indexStream, session);
                if (indexEvent == null || saga.OriginalMessageId != indexEvent.IndexEvent.OriginalMessageId)
                {
                    throw;
                }
            }
            var link = Json.UTF8NoBom.GetBytes($"1@{indexStream}");
            return new EventData(Guid.NewGuid(), "$>", false, link, new byte[0]);
        }

        static Task AddUniqueIndex(string indexStream, IContainSagaData saga, string originalMessageId, SynchronizedStorageSession session)
        {
            var indexPayload = new SagaIndexEvent(saga.Id, originalMessageId).ToJsonBytes();
            var indexEventData = new EventData(Guid.NewGuid(), SagaIndexEventType, true, indexPayload, new byte[0]);
            var dataEventData = new EventData(Guid.NewGuid(), SagaDataEventType, true, saga.ToJsonBytes(), new byte[0]);
            return session.AppendToStreamAsync(indexStream, ExpectedVersion.NoStream, indexEventData, dataEventData);
        }

        static Task AddMarker(string indexStream, IContainSagaData saga, SynchronizedStorageSession session)
        {
            var markerPayload = new SagaMarkerEvent(saga.Id, indexStream).ToJsonBytes();
            var markerEventData = new EventData(Guid.NewGuid(), SagaMarkerEventType, true, markerPayload, new byte[0]);
            return session.AppendToStreamAsync(BuildMarkerStreamName(saga.Id), ExpectedVersion.NoStream, markerEventData);
        }

        static async Task<SagaIndexEntry> ReadIndex(string indexStream, SynchronizedStorageSession session)
        {
            var slice = await session.ReadStreamEventsForwardAsync(indexStream, 0, 2, true).ConfigureAwait(false);
            if (slice.Status != SliceReadStatus.Success)
            {
                return null;
            }
            var indexEntry = slice.Events[0].Event.Data.ParseJson<SagaIndexEvent>();
            return new SagaIndexEntry(indexEntry, slice.Events[1].Event.Data);
        }

        public async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var messageId = context.Get<IncomingMessage>().MessageId;
            var streamName = BuildSagaStreamName(sagaData.GetType(), sagaData.Id);

            var stateChangeEvent = new EventData(Guid.NewGuid(), SagaDataEventType, true, sagaData.ToJsonBytes(), new byte[0]);

            var versionInfo = ((EventStoreSynchronizedStorageSession)session).GetSagaVersion(sagaData.Id);
            if (session.SupportsAtomicQueueForStore())
            {
                var alreadyLocked = versionInfo.AlreadyLocked ?? await CheckLock(messageId, streamName, sagaData, session).ConfigureAwait(false);
                if (!alreadyLocked)
                {
                    var lockEvent = new EventData(Guid.NewGuid(), SagaDataLockEventType, true, new SagaDataLockEvent(messageId).ToJsonBytes(), new byte[0]);
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

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var messageId = context.Get<IncomingMessage>().MessageId;
            var streamName = BuildSagaStreamName(typeof(TSagaData), sagaId);
            var result = await GetFromStream<TSagaData>(sagaId, messageId, streamName, session).ConfigureAwait(false);
            if (result != null)
            {
                return result;
            }
            var markerReadResult = await session.ReadStreamEventsForwardAsync(BuildMarkerStreamName(sagaId), 0, 1, false).ConfigureAwait(false);
            if (markerReadResult.Status != SliceReadStatus.Success)
            {
                return default(TSagaData);
            }
            //Marker exists, let's find out if index also exists
            var markerEvent = markerReadResult.Events[0].Event.Data.ParseJson<SagaMarkerEvent>();
            var index = await ReadIndex(markerEvent.SagaIndexStream, session);
            if (index == null)
            {
                Log.Warn($"A message arrived destined for saga {sagaId} but that saga does not exist in the store. There is however a marker indicating a failed attempt to store it. "
                         + "That saga likely generated a ghost message for which the current message is a reply. If the automatic retries are enabled the saga has probably been created "
                         + "under a different ID. In order to prevent ghost messages either ensure that the Outbox is enabled or, when creating a saga, first raise a timeout and send out "
                         + "messages from the handler of that timeout.");
            }
            else
            {
                throw new Exception($"Message {index.IndexEvent.OriginalMessageId} is still in process of persisting the saga instance {sagaId} which this message is destined to. Reverting the processing to wait for that other message.");
            }
            return default(TSagaData);
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
            var index = await ReadIndex(BuildIndexStreamName(typeof(TSagaData), propertyName, propertyValue), session);
            if (index == null)
            {
                return default(TSagaData);
            }
            var messageId = context.Get<IncomingMessage>().MessageId;
            var sagaId = index.IndexEvent.SagaId;
            var streamName = BuildSagaStreamName(typeof(TSagaData), sagaId);
            var sagaData = await GetFromStream<TSagaData>(sagaId, messageId, streamName, session).ConfigureAwait(false);
            if (sagaData != null)
            {
                return sagaData;
            }
            if (index.IndexEvent.OriginalMessageId != messageId)
            {
                throw new Exception(
                    $"Message {index.IndexEvent.OriginalMessageId} is still in process of persisting the saga instance {sagaId} which this message is destined to. Reverting the processing to wait for that other message.");
            }

            //Our message in the previous processing attempt managed to persist the index entry but not the entity entry
            sagaData = index.SagaData.ParseJson<TSagaData>();
            var versionInfo = new SagaVersion(ExpectedVersion.NoStream, false);
            ((EventStoreSynchronizedStorageSession)session).StoreSagaVersion(sagaId, versionInfo);
            return sagaData;
        }

        static string BuildIndexStreamName(Type sagaType, string property, object value)
        {
            return "nsb-saga-index-" + BuildSagaTypeName(sagaType) + "-by-" + property.ToLowerInvariant() + "#" + FormatValue(value);
        }

        static string BuildMarkerStreamName(Guid sagaId)
        {
            return "nsb-saga-marker-" + sagaId.ToString("N");
        }

        static string FormatValue(object value)
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

        static readonly ILog Log = LogManager.GetLogger<SagaPersister>();
    }
}