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

namespace NServiceBus
{
    class SagaPersister : ISagaPersister
    {
        const string SagaDataEventType = "$saga-data";
        const string SagaIndexEventType = "$saga-index";
        static readonly ConditionalWeakTable<IContainSagaData, SagaVersion> versionInformation = new ConditionalWeakTable<IContainSagaData, SagaVersion>();

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            if (correlationProperty != null)
            {
                await CreateIndicesForSaga(sagaData, correlationProperty, session.Connection()).ConfigureAwait(false);
            }
            await SaveData(sagaData, session.Connection()).ConfigureAwait(false);
        }

        async Task CreateIndicesForSaga(IContainSagaData saga, SagaCorrelationProperty correlationProperty, IEventStoreConnection connection)
        {
            var sagaType = saga.GetType();
            var propertyName = correlationProperty.Name;
            var propertyValue = correlationProperty.Value;
            try
            {
                await AddUniqueIndex(saga.Id, sagaType, saga.OriginalMessageId, propertyName, propertyValue, connection).ConfigureAwait(false);
            }
            catch (AggregateException ex)
            {
                var versionException = ex.Flatten().InnerException as WrongExpectedVersionException;
                if (versionException != null)
                {
                    var indexEvent = await ReadIndex(sagaType, propertyName, propertyValue, connection);
                    if (indexEvent == null || saga.OriginalMessageId != indexEvent.OriginalMessageId)
                    {
                        throw;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        Task AddUniqueIndex(Guid sagaId, Type sagaType, string originalMessageId, string property, object value, IEventStoreConnection connection)
        {
            var indexStream = BuildIndexStreamName(sagaType, property, value);
            var payload = new SagaIndexEvent(sagaId, originalMessageId).ToJsonBytes();
            var eventData = new EventData(Guid.NewGuid(), SagaIndexEventType, true, payload, new byte[0]);
            return connection.AppendToStreamAsync(indexStream, ExpectedVersion.NoStream, eventData);
        }

        async Task<SagaIndexEvent> ReadIndex(Type sagaType, string property, object value, IEventStoreConnection connection)
        {
            var indexStream = BuildIndexStreamName(sagaType, property, value);
            var slice = await connection.ReadStreamEventsForwardAsync(indexStream, 0, 1, true).ConfigureAwait(false);
            if (slice.Status != SliceReadStatus.Success)
            {
                return null;
            }
            var indexEvent = slice.Events[0];
            var payload = indexEvent.Event.Data.ParseJson<SagaIndexEvent>();
            return payload;
        }

        static Task SaveData(IContainSagaData saga, IEventStoreConnection connection)
        {
            var streamName = BuildSagaStreamName(saga.GetType(), saga.Id);
            var eventData = new EventData(Guid.NewGuid(), SagaDataEventType, true, saga.ToJsonBytes(), new byte[0]);
            return connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, eventData);
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var streamName = BuildSagaStreamName(sagaData.GetType(), sagaData.Id);
            var eventData = new EventData(Guid.NewGuid(), SagaDataEventType, true, sagaData.ToJsonBytes(), new byte[0]);

            SagaVersion versionInfo;
            versionInformation.TryGetValue(sagaData, out versionInfo);

            var expectedVersion = versionInfo != null
                                      ? versionInfo.Version
                                      : ExpectedVersion.Any;

            return session.Connection().AppendToStreamAsync(streamName, expectedVersion, eventData);
        }


        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var streamName = BuildSagaStreamName(typeof(TSagaData), sagaId);
            return GetFromStream<TSagaData>(streamName, session.Connection());
        }

        async Task<TSagaData> GetFromStream<TSagaData>(string streamName, IEventStoreConnection connection) where TSagaData : IContainSagaData
        {
            var lastVersion = await connection.ReadStreamEventsBackwardAsync(streamName, -1, 1, true).ConfigureAwait(false);
            if (lastVersion.Status == SliceReadStatus.Success)
            {
                var evnt = lastVersion.Events[0].Event;
                var saga = evnt.Data.ParseJson<TSagaData>();
                var versionInfo = new SagaVersion(evnt.EventNumber);
                versionInformation.Add(saga, versionInfo);
                return saga;
            }
            return default(TSagaData);
        }


        public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var index = await ReadIndex(typeof(TSagaData), propertyName, propertyValue, session.Connection());
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
            return session.Connection().DeleteStreamAsync(streamName, ExpectedVersion.Any, true);
        }

        static string BuildSagaStreamName(Type sagaType, Guid sagaId)
        {
            return "nsb-saga-" + BuildSagaTypeName(sagaType) + "-" + sagaId.ToString("N");
        }

        static string BuildSagaTypeName(Type sagaType)
        {
            return sagaType.FullName.ToLowerInvariant().Replace(".","-");
        }
    }
}