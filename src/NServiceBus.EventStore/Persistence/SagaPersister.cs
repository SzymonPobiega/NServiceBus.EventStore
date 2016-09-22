using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using NServiceBus.Persistence.EventStore.SagaPersister;
using NServiceBus.Sagas;
using NServiceBus.Transport;

namespace NServiceBus
{
    class SagaPersister : ISagaPersister
    {
        internal const string SagaDataEventType = "$saga-data";
        internal const string SagaDataLockEventType = "$saga-data-lock";
        internal const string SagaIndexEventType = "$saga-index";
        internal const string SagaMarkerEventType = "$saga-marker";

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var operations = new SagaPersisterAtomicOperations(session);

            var streamName = BuildSagaStreamName(sagaData.GetType(), sagaData.Id);
            return SaveWithIndex(sagaData, correlationProperty, operations, streamName);
        }

        static async Task SaveWithIndex(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SagaPersisterAtomicOperations operations, string streamName)
        {
            var propertyValue = correlationProperty.Value;
            var indexStream = BuildIndexStreamName(sagaData.GetType(), propertyValue);

            await operations.CreateMarker(indexStream, sagaData).ConfigureAwait(false);
            var linkEvent = await CreateIndex(sagaData, indexStream, operations).ConfigureAwait(false);
            await operations.SaveSagaDataLink(streamName, linkEvent).ConfigureAwait(false);
            await operations.DeleteMarker(sagaData).ConfigureAwait(false);
        }
        
        static async Task<EventData> CreateIndex(IContainSagaData saga, string indexStream, SagaPersisterAtomicOperations operations)
        {
            try
            {
                await operations.CreateIndex(indexStream, saga, saga.OriginalMessageId).ConfigureAwait(false);
            }
            catch (WrongExpectedVersionException)
            {
                var indexEvent = await operations.ReadIndex(indexStream).ConfigureAwait(false);
                if (indexEvent == null || saga.OriginalMessageId != indexEvent.IndexEvent.OriginalMessageId)
                {
                    throw;
                }
            }
            var link = Json.UTF8NoBom.GetBytes($"1@{indexStream}");
            return new EventData(Guid.NewGuid(), "$>", false, link, new byte[0]);
        }

        public async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var operations = new SagaPersisterAtomicOperations(session);
            var messageId = context.Get<IncomingMessage>().MessageId;
            var streamName = BuildSagaStreamName(sagaData.GetType(), sagaData.Id);
            var versionInfo = operations.GetSagaVersion(sagaData.Id);

            await SaveSagaData(sagaData, session, operations, messageId, streamName, versionInfo);
        }

        static async Task SaveSagaData(IContainSagaData sagaData, SynchronizedStorageSession session, SagaPersisterAtomicOperations operations, string messageId, string streamName, SagaVersion versionInfo)
        {
            var stateChangeEvent = new EventData(Guid.NewGuid(), SagaDataEventType, true, sagaData.ToJsonBytes(), new byte[0]);

            if (session.SupportsAtomicQueueForStore())
            {
                var alreadyLocked = versionInfo.AlreadyLocked ??
                                    await operations.CheckLock(messageId, streamName, sagaData).ConfigureAwait(false);
                if (!alreadyLocked)
                {
                    await operations.CreateLock(messageId, streamName, versionInfo).ConfigureAwait(false);
                }
                session.AtomicQueueForStore(streamName, stateChangeEvent);
            }
            else
            {
                await operations.SaveSagaData(streamName, versionInfo, stateChangeEvent).ConfigureAwait(false);
            }
        }

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var operations = new SagaPersisterAtomicOperations(session);
            var messageId = context.Get<IncomingMessage>().MessageId;
            var streamName = BuildSagaStreamName(typeof(TSagaData), sagaId);
            var result = await operations.ReadSagaData<TSagaData>(sagaId, messageId, streamName).ConfigureAwait(false);
            if (result != null)
            {
                return result;
            }
            var markerEvent = await operations.ReadMarker(sagaId).ConfigureAwait(false);
            if (markerEvent == null)
            {
                return default(TSagaData);
            }
            //Marker exists, let's find out if index also exists
            var index = await operations.ReadIndex(markerEvent.SagaIndexStream).ConfigureAwait(false);
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


        public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var operations = new SagaPersisterAtomicOperations(session);
            var index = await operations.ReadIndex(BuildIndexStreamName(typeof(TSagaData), propertyValue)).ConfigureAwait(false);
            if (index == null)
            {
                return default(TSagaData);
            }
            var messageId = context.Get<IncomingMessage>().MessageId;
            var sagaId = index.IndexEvent.SagaId;
            var streamName = BuildSagaStreamName(typeof(TSagaData), sagaId);
            var sagaData = await operations.ReadSagaData<TSagaData>(sagaId, messageId, streamName).ConfigureAwait(false);
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
            operations.StoreSagaVersion(sagaId, versionInfo);
            return sagaData;
        }

        static string BuildIndexStreamName(Type sagaType, object value)
        {
            return "nsb-saga-index-" + BuildSagaTypeName(sagaType) + "-" + FormatValue(value);
        }

        static string FormatValue(object value)
        {
            return value.ToString();
        }

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var streamName = BuildSagaStreamName(sagaData.GetType(), sagaData.Id);
            var indexLinkEvent = await session.ReadStreamEventsForwardAsync(streamName, 0, 1, false).ConfigureAwait(false);
            var indexEntry = Json.UTF8NoBom.GetString(indexLinkEvent.Events[0].Event.Data);
            var indexStream = indexEntry.Split('@')[1];
            await session.DeleteStreamAsync(streamName, ExpectedVersion.Any, true).ConfigureAwait(false);
            await session.DeleteStreamAsync(indexStream, ExpectedVersion.Any, true).ConfigureAwait(false);
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