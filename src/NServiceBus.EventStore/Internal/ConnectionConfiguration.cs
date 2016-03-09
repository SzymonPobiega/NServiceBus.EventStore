using System;
using System.Net;
using EventStore.ClientAPI;
using NServiceBus.Internal.Projections;

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
                throw new ArgumentNullException("connectionSettings");
            }
            if (httpEndpoint == null)
            {
                throw new ArgumentNullException("httpEndpoint");
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

        public IEventStoreConnection CreateConnection()
        {
            if (clusterSettings != null)
            {
                return EventStoreConnection.Create(connectionSettings, clusterSettings, name);
            }
            return EventStoreConnection.Create(connectionSettings, singleNodeAddress, name);
        }

        public IProjectionsManager CreateProjectionsManager()
        {
            return new DefaultProjectionsManager(new ProjectionsManager(new NoopLogger(), httpEndpoint, TimeSpan.FromSeconds(90)), connectionSettings.DefaultUserCredentials);
        }
    }
}