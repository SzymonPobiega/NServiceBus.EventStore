//using System;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Text.RegularExpressions;
//using EventStore.ClientAPI;
//using EventStore.ClientAPI.Exceptions;
//using NServiceBus.Saga;
//using NServiceBus.Transports.EventStore;
//using NServiceBus.Transports.EventStore.Serializers.Json;

//namespace NServiceBus.Persistence.EventStore.SagaPersister
//{
//    public class EventStoreSagaPersister : ISagaPersister
//    {
//        private const string SagaDataEventType = "SagaData";
//        private const string SagaIndexEventType = "SagaIndex";
//        private readonly IManageEventStoreConnections connectionManager;
//        private static readonly ConditionalWeakTable<IContainSagaData, SagaVersion> versionInformation = new ConditionalWeakTable<IContainSagaData, SagaVersion>();

//        public EventStoreSagaPersister(IManageEventStoreConnections connectionManager)
//        {
//            this.connectionManager = connectionManager;
//        }

//        public void Save(IContainSagaData saga)
//        {
//            CreateIndicesForSaga(saga);
//            SaveData(saga);
//        }

//        private void CreateIndicesForSaga(IContainSagaData saga)
//        {
//            var sagaType = saga.GetType();
//            var propertiesToIndex = Features.Sagas.SagaEntityToMessageToPropertyLookup[sagaType]
//                .Select(x => x.Value.Key)
//                .Distinct()
//                .OrderBy(x => x.Name);

//            foreach (var property in propertiesToIndex)
//            {
//                var propertyValue = property.GetValue(saga, new object[0]);
//                try
//                {
//                    AddUniqueIndex(saga.Id, sagaType, saga.OriginalMessageId, property.Name, propertyValue);
//                }
//                catch (AggregateException ex)
//                {
//                    var versionException = ex.Flatten().InnerException as WrongExpectedVersionException;
//                    if (versionException != null)
//                    {
//                        var indexEvent = ReadIndex(sagaType, property.Name, propertyValue);
//                        if (saga.OriginalMessageId != indexEvent.OriginalMessageId)
//                        {
//                            throw;
//                        }
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }
//            }
//        }

//        private void AddUniqueIndex(Guid sagaId, Type sagaType, string originalMessageId, string property, object value)
//        {
//            var indexStream = BuildIndexStreamName(sagaType, property, value);
//            var payload = new SagaIndexEvent(sagaId, originalMessageId).ToJsonBytes();
//            var eventData = new EventData(Guid.NewGuid(), SagaIndexEventType, true, payload, new byte[0]);
//            connectionManager.GetConnection().AppendToStreamAsync(indexStream, ExpectedVersion.NoStream, eventData).Wait();
//        }

//        private SagaIndexEvent ReadIndex(Type sagaType, string property, object value)
//        {
//            var indexStream = BuildIndexStreamName(sagaType, property, value);
//            var indexEvent = connectionManager.GetConnection().ReadStreamEventsForwardAsync(indexStream, 0, 1, true).Result.Events[0];
//            var payload = indexEvent.Event.Data.ParseJson<SagaIndexEvent>();
//            return payload;
//        }

//        private void SaveData(IContainSagaData saga)
//        {
//            var streamName = BuildSagaStreamName(saga.GetType(), saga.Id);
//            var eventData = new EventData(Guid.NewGuid(), SagaDataEventType, true, saga.ToJsonBytes(), new byte[0]);
//            connectionManager.GetConnection().AppendToStreamAsync(streamName, ExpectedVersion.NoStream, eventData).Wait();
//        }

//        public void Update(IContainSagaData saga)
//        {
//            var streamName = BuildSagaStreamName(saga.GetType(), saga.Id);
//            var eventData = new EventData(Guid.NewGuid(), SagaDataEventType, true, saga.ToJsonBytes(), new byte[0]);

//            SagaVersion versionInfo;
//            versionInformation.TryGetValue(saga, out versionInfo);

//            var expectedVersion = versionInfo != null
//                                      ? versionInfo.Version
//                                      : ExpectedVersion.Any;

//            connectionManager.GetConnection().AppendToStreamAsync(streamName, expectedVersion, eventData).Wait();
//        }

//        public T Get<T>(Guid sagaId) where T : IContainSagaData
//        {
//            var streamName = BuildSagaStreamName(typeof(T), sagaId);
//            return GetFromStream<T>(streamName);
//        }

//        private T GetFromStream<T>(string streamName) where T : IContainSagaData
//        {
//            var lastVersion = connectionManager.GetConnection().ReadStreamEventsBackwardAsync(streamName, -1, 1, true).Result;
//            if (lastVersion.Status == SliceReadStatus.Success)
//            {
//                var evnt = lastVersion.Events[0].Event;
//                var saga = evnt.Data.ParseJson<T>();
//                var versionInfo = new SagaVersion(evnt.EventNumber);
//                versionInformation.Add(saga, versionInfo);
//                return saga;
//            }
//            return default(T);
//        }

//        public T Get<T>(string property, object value) where T : IContainSagaData
//        {
//            var index = ReadIndex(typeof (T), property, value);
//            return Get<T>(index.SagaId);
//        }

//        private string BuildIndexStreamName(Type sagaType, string property, object value)
//        {
//            return "SagaIndex-" + sagaType.FullName + "_by_" + property + "#" + FormatValue(value);
//        }

//        private string FormatValue(object value)
//        {
//            return value.ToString();
//        }

//        public void Complete(IContainSagaData saga)
//        {
//            var streamName = BuildSagaStreamName(saga.GetType(), saga.Id);
//            connectionManager.GetConnection().DeleteStreamAsync(streamName, ExpectedVersion.Any, true).Wait();
//        }

//        private static string BuildSagaStreamName(Type sagaType, Guid sagaId)
//        {
//            return BuildCategoryName(sagaType) + "-" + sagaId.ToString("N");
//        }

//        private static string BuildCategoryName(Type sagaType)
//        {
//            return "Saga_" + sagaType.FullName;
//        }
//    }
//}