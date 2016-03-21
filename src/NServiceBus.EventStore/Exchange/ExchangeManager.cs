using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace NServiceBus.Exchange
{
    class ExchangeManager
    {
        bool enableCaching;
        ExchangeRepository repository;
        volatile ExchangeCollection cachedExchangeCollection;

        public ExchangeManager(IEventStoreConnection connection, bool enableCaching = true)
        {
            this.enableCaching = enableCaching;
            repository = new ExchangeRepository(connection);
        }

        public async Task Start()
        {
            var data = await repository.LoadExchanges().ConfigureAwait(false);
            cachedExchangeCollection = new ExchangeCollection(data);

            await repository.StartMonitoring(OnNewVersion).ConfigureAwait(false);
        }

        void OnNewVersion(ExchangeDataCollection newData)
        {
            cachedExchangeCollection = new ExchangeCollection(newData);
        }

        public void Stop()
        {
            repository.StopMonitoring();
        }

        public async Task UpdateExchanges(Action<ExchangeDataCollection> updateAction)
        {
            await repository.UpdateExchanges(updateAction).ConfigureAwait(false);
        }

        public Task DeclareExchange(string name)
        {
            return repository.DeclareExchange(name);
        }

        public Task BindExchange(string upstream, string downstream)
        {
            return repository.BindExchange(upstream, downstream);
        }

        public Task BindQueue(string exchange, string queue)
        {
            return repository.BindQueue(exchange, queue);
        }

        public Task UnbindQueue(string exchange, string queue)
        {
            return repository.UnbindQueue(exchange, queue);
        }

        public async Task<IEnumerable<string>> GetDestinationQueues(string destinationExchange)
        {
            var copy = enableCaching 
                ? cachedExchangeCollection 
                : new ExchangeCollection(await repository.LoadExchanges().ConfigureAwait(false));
            return copy.GetDestinationQueues(destinationExchange);
        }
    }
}