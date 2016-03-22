using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Persistence;

namespace NServiceBus
{
    class EventStoreSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
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
            return Task.FromResult(0);
        }

        public IEventStoreConnection Connection { get; }
    }
}