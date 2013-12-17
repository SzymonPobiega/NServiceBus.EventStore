using System;
using System.Runtime.CompilerServices;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Utils;
using NServiceBus.Saga;
using NServiceBus.Transports.EventStore;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class EventStoreSagaPersister : IPersistSagas
    {
        private const string SagaDataEventType = "SagaData";
        private readonly IManageEventStoreConnections connectionManager;
        private static readonly ConditionalWeakTable<IContainSagaData, SagaVersion> versionInformation = new ConditionalWeakTable<IContainSagaData, SagaVersion>(); 

        public EventStoreSagaPersister(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public void Save(IContainSagaData saga)
        {
            var streamName = BuildSagaStreamName(saga.GetType(), saga.Id);
            var eventData = new EventData(Guid.NewGuid(), SagaDataEventType, true, saga.ToJsonBytes(), new byte[0]);
            connectionManager.GetConnection().AppendToStream(streamName, ExpectedVersion.NoStream, eventData);
        }

        public void Update(IContainSagaData saga)
        {
            var streamName = BuildSagaStreamName(saga.GetType(), saga.Id);
            var eventData = new EventData(Guid.NewGuid(), SagaDataEventType, true, saga.ToJsonBytes(), new byte[0]);

            SagaVersion versionInfo;
            versionInformation.TryGetValue(saga, out versionInfo);

            int expectedVersion = versionInfo != null
                                      ? versionInfo.Version
                                      : ExpectedVersion.Any;

            connectionManager.GetConnection().AppendToStream(streamName, expectedVersion, eventData);
        }

        public T Get<T>(Guid sagaId) where T : IContainSagaData
        {
            var streamName = BuildSagaStreamName(typeof(T), sagaId);
            return GetFromStream<T>(streamName);
        }

        private T GetFromStream<T>(string streamName) where T : IContainSagaData
        {
            var lastVersion = connectionManager.GetConnection().ReadStreamEventsBackward(streamName, -1, 1, true);
            if (lastVersion.Status == SliceReadStatus.Success)
            {
                var evnt = lastVersion.Events[0].Event;
                var saga = evnt.Data.ParseJson<T>();
                var versionInfo = new SagaVersion(evnt.EventNumber);
                versionInformation.Add(saga, versionInfo);
                return saga;
            }
            return default(T);
        }

        public T Get<T>(string property, object value) where T : IContainSagaData
        {
            var indexStreamName = typeof (T).FullName + "_by_" + property + "-" + value;
            return GetFromStream<T>(indexStreamName);
        }

        public void Complete(IContainSagaData saga)
        {
            var streamName = BuildSagaStreamName(saga.GetType(), saga.Id);
            connectionManager.GetConnection().DeleteStream(streamName, ExpectedVersion.Any,true);
        }

        private static string BuildSagaStreamName(Type sagaType, Guid sagaId)
        {
            return BuildCategoryName(sagaType) + "-" + sagaId.ToString("N");
        }

        private static string BuildCategoryName(Type sagaType)
        {
            return "Saga_" + sagaType.FullName;
        }
    }
}