using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Persistence;
using NServiceBus.Persistence.EventStore.SagaPersister;

namespace NServiceBus
{
    class EventStoreSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public IEventStoreConnection Connection { get; }

        public EventStoreSynchronizedStorageSession(IEventStoreConnection connection)
        {
            Connection = connection;
        }

        public void Dispose()
        {
            //NOOP.
        }

        public Task CompleteAsync()
        {
            return TaskEx.CompletedTask;
        }


        public virtual bool SupportsAtomicAppend => false;

        public void StoreSagaVersion(Guid sagaId, SagaVersion version)
        {
            sagaVersions[sagaId] = version;
        }

        public SagaVersion GetSagaVersion(Guid sagaId)
        {
            SagaVersion result;
            if (!sagaVersions.TryGetValue(sagaId, out result))
            {
                throw new Exception($"No optimistic concurrency information found for saga {sagaId}. If you use custom saga finder please use SynchronizedStorageSession.ProvideSagaVersion method to provide that information based on the storage query result.");
            }
            return result;
        }

        Dictionary<Guid, SagaVersion> sagaVersions = new Dictionary<Guid, SagaVersion>(); 
    }
}