using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Transports;

namespace NServiceBus
{
    class SynchronizedStorage : ISynchronizedStorageAdapter, ISynchronizedStorage
    {
        static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult<CompletableSynchronizedStorageSession>(null);

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            var typedTransaction = transaction as EventStoreOutboxTransaction;
            return typedTransaction != null 
                ? Task.FromResult<CompletableSynchronizedStorageSession>(new OutboxEventStoreSynchronizedStorageSession(typedTransaction.Connection, typedTransaction)) 
                : EmptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            IEventStoreConnection connection;
            if (!transportTransaction.TryGet(out connection))
            {
                throw new Exception("EventStore persistence can only be used with EventStore transport.");
            }
            return Task.FromResult<CompletableSynchronizedStorageSession>(new EventStoreSynchronizedStorageSession(connection));
        }
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            throw new Exception("EventStore persistence can only be used with EventStore transport.");
        }
    }
}