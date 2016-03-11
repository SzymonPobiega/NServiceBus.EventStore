using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Internal.Projections;

namespace NServiceBus.Internal
{
    public class DefaultConnectionManager : IManageEventStoreConnections, IDisposable
    {
        private readonly IConnectionConfiguration connectionConfiguration;
        private readonly IEventStoreConnection connection;
        private bool connected;

        public DefaultConnectionManager(IConnectionConfiguration connectionConfiguration)
        {
            this.connectionConfiguration = connectionConfiguration;
            connection = this.connectionConfiguration.CreateConnection();
        }

        public async Task Connect()
        {
            if (connected)
            {
                return;
            }
            await connection.ConnectAsync();
            connected = true;
        }
        
        public IEventStoreConnection GetConnection()
        {
            return connection;
        }

        public IProjectionsManager GetProjectionManager()
        {
            return connectionConfiguration.CreateProjectionsManager();
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            connection.Dispose();
        }
    }
}