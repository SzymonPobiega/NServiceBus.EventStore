using System;
using EventStore.ClientAPI;
using NServiceBus.Transports.EventStore.Config;
using NServiceBus.Transports.EventStore.Projections;

namespace NServiceBus.Transports.EventStore
{
    public class DefaultConnectionManager : IManageEventStoreConnections, IDisposable
    {
        private readonly IConnectionConfiguration connectionConfiguration;
        private readonly Lazy<IEventStoreConnection> connection;

        public DefaultConnectionManager(IConnectionConfiguration connectionConfiguration)
        {
            this.connectionConfiguration = connectionConfiguration;
            connection = new Lazy<IEventStoreConnection>(() =>
                {
                    var conn = this.connectionConfiguration.CreateConnection();
                    conn.ConnectAsync().Wait();
                    return conn;
                }, true);
        }

        public IEventStoreConnection GetConnection()
        {
            return connection.Value;
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
            if (connection.IsValueCreated)
            {
                connection.Value.Dispose();
            }
        }
    }
}