using System;
using System.Net;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Projections;

namespace NServiceBus.Internal
{
    public class ConnectionConfiguration : IConnectionConfiguration
    {
        private readonly ConnectionSettings connectionSettings;
        private readonly ClusterSettings clusterSettings;
        private readonly IPEndPoint singleNodeAddress;
        private readonly IPEndPoint httpEndpoint;
        private readonly string name;

        public ConnectionConfiguration(
            ConnectionSettings connectionSettings,
            ClusterSettings clusterSettings,
            IPEndPoint singleNodeAddress,
            IPEndPoint httpEndpoint,
            string name)
        {
            if (connectionSettings == null)
            {
                throw new ArgumentNullException(nameof(connectionSettings));
            }
            if (httpEndpoint == null)
            {
                throw new ArgumentNullException(nameof(httpEndpoint));
            }
            if (clusterSettings == null && singleNodeAddress == null)
            {
                throw new ArgumentException("Either clusterSettings or singleNodeAddress must be specified.");
            }
            if (clusterSettings != null && singleNodeAddress != null)
            {
                throw new ArgumentException("Either clusterSettings or singleNodeAddress must be specified but not both.");
            }
            this.connectionSettings = connectionSettings;
            this.clusterSettings = clusterSettings;
            this.singleNodeAddress = singleNodeAddress;
            this.httpEndpoint = httpEndpoint;
            this.name = name;
        }

        public IEventStoreConnection CreateConnection(string providedName)
        {
            var conn = clusterSettings != null 
                ? EventStoreConnection.Create(connectionSettings, clusterSettings) 
                : EventStoreConnection.Create(connectionSettings, singleNodeAddress);
            conn.Connected += (sender, args) =>
            {
                Console.WriteLine(">> Connection {0}: {1}", args.Connection.ConnectionName, providedName);
            };
            return conn;
        }
    }
}