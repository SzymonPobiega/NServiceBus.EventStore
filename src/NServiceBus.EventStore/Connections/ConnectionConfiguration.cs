using System;
using System.Net;
using EventStore.ClientAPI;

namespace NServiceBus.Internal
{
    public class ConnectionConfiguration : IConnectionConfiguration
    {
        ConnectionSettings connectionSettings;
        ClusterSettings clusterSettings;
        IPEndPoint singleNodeAddress;

        public ConnectionConfiguration(
            ConnectionSettings connectionSettings,
            ClusterSettings clusterSettings,
            IPEndPoint singleNodeAddress)
        {
            if (connectionSettings == null)
            {
                throw new ArgumentNullException(nameof(connectionSettings));
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