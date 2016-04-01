using System;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

namespace NServiceBus.EventStore.Tests
{
    class FaultySagaPersister : ISagaPersister
    {
        readonly ISagaPersister persister;

        public FaultySagaPersister(ISagaPersister persister)
        {
            this.persister = persister;
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session,
            ContextBag context)
        {
            try
            {
                await persister.Save(sagaData, correlationProperty, session, context).ConfigureAwait(false);
            }
            catch (ChaosException)
            {
            }
        }

        public async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            try
            {
                await persister.Update(sagaData, session, context).ConfigureAwait(false);
            }
            catch (ChaosException)
            {
            }
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            return persister.Get<TSagaData>(sagaId, session, context);
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            return persister.Get<TSagaData>(propertyName, propertyValue, session, context);
        }

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            try
            {
                await persister.Complete(sagaData, session, context).ConfigureAwait(false);
            }
            catch (ChaosException)
            {
            }
        }
    }
}