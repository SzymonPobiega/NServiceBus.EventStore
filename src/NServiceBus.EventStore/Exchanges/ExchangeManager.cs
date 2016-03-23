using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Internal;

namespace NServiceBus
{
    class ExchangeManager
    {
        IConnectionConfiguration connectionConfiguration;
        bool enableCaching;
        volatile ExchangeCollection cachedExchangeCollection;
        ExchangeMonitor monitor;
        IEventStoreConnection monitorConnection;

        public ExchangeManager(IConnectionConfiguration connectionConfiguration, bool enableCaching = true)
        {
            this.connectionConfiguration = connectionConfiguration;
            this.enableCaching = enableCaching;
        }

        public async Task Start(CriticalError criticalError)
        {
            var data = await LoadExchanges().ConfigureAwait(false);
            cachedExchangeCollection = new ExchangeCollection(data);

            monitorConnection = connectionConfiguration.CreateConnection("ExchangeMonitor");
            await monitorConnection.ConnectAsync().ConfigureAwait(false);

            monitor = new ExchangeMonitor(monitorConnection, OnNewVersion, criticalError);
            monitor.StartMonitoring();
        }

        void OnNewVersion(ExchangeDataCollection newData)
        {
            cachedExchangeCollection = new ExchangeCollection(newData);
        }

        public void Stop()
        {
            monitor.StopMonitoring();
            monitorConnection.EnsureClosed();
        }

        public async Task UpdateExchanges(Action<ExchangeDataCollection> updateAction)
        {
            using (var connection = connectionConfiguration.CreateConnection("UpdateExchanges"))
            {
                await connection.ConnectAsync().ConfigureAwait(false);
                var repo = new ExchangeRepository(connection);
                await repo.UpdateExchanges(updateAction).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<string>> GetDestinationQueues(string destinationExchange)
        {
            var copy = enableCaching 
                ? cachedExchangeCollection 
                : new ExchangeCollection(await LoadExchanges());
            return copy.GetDestinationQueues(destinationExchange);
        }

        async Task<ExchangeDataCollection> LoadExchanges()
        {
            ExchangeRepository repo;
            using (var connection = connectionConfiguration.CreateConnection("LoadExchanges"))
            {
                await connection.ConnectAsync().ConfigureAwait(false);
                repo = new ExchangeRepository(connection);
                return await repo.LoadExchanges().ConfigureAwait(false);
            }
        }
    }
}