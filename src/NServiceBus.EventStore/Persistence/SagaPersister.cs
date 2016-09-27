using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
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

            return correlationProperty != null
                ? SaveSagaWithCorrelationProperty(sagaData, correlationProperty, operations, session)
                : SaveSagaWithoutCorrelationProperty(sagaData, session);
        }

        static async Task SaveSagaWithCorrelationProperty(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SagaPersisterAtomicOperations operations, SynchronizedStorageSession session)
        {
            var propertyValue = correlationProperty.Value;
            var indexStream = BuildSagaByIdStreamName(sagaData.GetType(), sagaData.Id);
            var dataStream = BuildSagaDataStreamName(sagaData.GetType(), propertyValue);
            var stateChangeEvent = new EventData(Guid.NewGuid(), SagaDataEventType, true, sagaData.ToJsonBytes(), new byte[0]);

            await operations.CreateIndex(indexStream, dataStream).ConfigureAwait(false);
            await session.AppendToStreamAsync(dataStream, ExpectedVersion.NoStream, stateChangeEvent).ConfigureAwait(false);
        }

        static Task SaveSagaWithoutCorrelationProperty(IContainSagaData sagaData, SynchronizedStorageSession session)
        {
            var indexStream = BuildSagaByIdStreamName(sagaData.GetType(), sagaData.Id);
            var stateChangeEvent = new EventData(Guid.NewGuid(), SagaDataEventType, true, sagaData.ToJsonBytes(), new byte[0]);

            return session.AppendToStreamAsync(indexStream, ExpectedVersion.NoStream, stateChangeEvent);
        }
        
        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var operations = new SagaPersisterAtomicOperations(session);
            var versionInfo = operations.GetSagaVersion(sagaData.Id);

            var dataStream = versionInfo.StreamName;
            var stateChangeEvent = new EventData(Guid.NewGuid(), SagaDataEventType, true, sagaData.ToJsonBytes(), new byte[0]);

            if (session.SupportsOutbox())
            {
                var outboxLink = session.AtomicQueueForStore(dataStream, stateChangeEvent);
                return session.AppendToStreamAsync(dataStream, versionInfo.Version, outboxLink);
            }
            return session.AppendToStreamAsync(dataStream, versionInfo.Version, stateChangeEvent);
        }

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var operations = new SagaPersisterAtomicOperations(session);
            var incomingMessage = context.Get<IncomingMessage>();
            var messageId = incomingMessage.MessageId;
            var indexStream = BuildSagaByIdStreamName(typeof(TSagaData), sagaId);
            var slice = await session.ReadStreamEventsBackwardAsync(indexStream, -1, 1, true).ConfigureAwait(false);
            if (slice.Status != SliceReadStatus.Success)
            {
                return default(TSagaData);
            }
            if (slice.Events[0].Event.EventType != SagaIndexEventType)
            {
                return await operations.ReadSagaData<TSagaData>(indexStream, slice.Events[0], messageId);
            }
            var indexEntry = slice.Events[0].Event.Data.ParseJson<SagaIndexEvent>();
            var dataStream = indexEntry.DataStreamName;
            slice = await session.ReadStreamEventsBackwardAsync(dataStream, -1, 1, true).ConfigureAwait(false);
            if (slice.Status != SliceReadStatus.Success)
            {
                return default(TSagaData);
            }
            var sagaData = await operations.ReadSagaData<TSagaData>(dataStream, slice.Events[0], messageId);
            if (sagaData != null)
            {
                return sagaData;
            }
            throw new Exception($"Either the saga has not yet been created successfully or saga ID {sagaId} has been sent out in a ghost message.");
        }

        public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var operations = new SagaPersisterAtomicOperations(session);
            var incomingMessage = context.Get<IncomingMessage>();
            var messageId = incomingMessage.MessageId;
            var streamName = BuildSagaDataStreamName(typeof(TSagaData), propertyValue);
            var slice = await session.ReadStreamEventsBackwardAsync(streamName, -1, 1, true).ConfigureAwait(false);
            if (slice.Status != SliceReadStatus.Success)
            {
                return default(TSagaData);
            }
            return await operations.ReadSagaData<TSagaData>(streamName, slice.Events[0], messageId);
        }

        static string BuildSagaDataStreamName(Type sagaType, object correlationPropertyValue)
        {
            return "nsb-saga-" + BuildSagaTypeName(sagaType) + "-" + FormatValue(correlationPropertyValue);
        }

        static string FormatValue(object value)
        {
            return value.ToString();
        }

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var indexStream = BuildSagaByIdStreamName(sagaData.GetType(), sagaData.Id);
            var slice = await session.ReadStreamEventsBackwardAsync(indexStream, -1, 1, false).ConfigureAwait(false);
            if (slice.Status != SliceReadStatus.Success)
            {
                return;
            }
            if (slice.Events[0].Event.EventType != SagaIndexEventType)
            {
                await session.DeleteStreamAsync(indexStream, ExpectedVersion.Any, true).ConfigureAwait(false);
                return;
            }
            var indexEntry = slice.Events[0].Event.Data.ParseJson<SagaIndexEvent>();
            var dataStream = indexEntry.DataStreamName;
            await session.DeleteStreamAsync(dataStream, ExpectedVersion.Any, true).ConfigureAwait(false);
            await session.DeleteStreamAsync(indexStream, ExpectedVersion.Any, true).ConfigureAwait(false);
        }

        static string BuildSagaByIdStreamName(Type sagaType, Guid sagaId)
        {
            return "nsb-saga-id-" + BuildSagaTypeName(sagaType) + "-" + sagaId.ToString("N");
        }

        static string BuildSagaTypeName(Type sagaType)
        {
            return sagaType.FullName.ToLowerInvariant().Replace(".", "-");
        }

        static readonly ILog Log = LogManager.GetLogger<SagaPersister>();
    }
}