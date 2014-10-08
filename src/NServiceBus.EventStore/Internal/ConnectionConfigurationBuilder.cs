using System.Net;
using EventStore.ClientAPI;

namespace NServiceBus.Internal
{
    public class ConnectionConfigurationBuilder
    {
        private readonly ConnectionSettingsBuilder connectionSettingsBuilder = global::EventStore.ClientAPI.ConnectionSettings.Create();
        private ClusterSettingsBuilder clusterSettingsBuilder;

        public ConnectionSettingsBuilder ConnectionSettings
        {
            get { return connectionSettingsBuilder; }
        }

        public ClusterSettingsBuilder ClusterSettings
        {
            get {
                return clusterSettingsBuilder ??
                       (clusterSettingsBuilder = global::EventStore.ClientAPI.ClusterSettings.Create());
            }
        }

        public string SingleNodeAddress { get; set; }
        public int? SingleNodePort{ get; set; }
        public string HttpEndpointAddress { get; set; }
        public int? HttpPort { get; set; }
        public string Name { get; set; }

        public ConnectionConfiguration Build()
        {
            var singleNodeAddress = SingleNodeAddress != null 
                ? new IPEndPoint(IPAddress.Parse(SingleNodeAddress), SingleNodePort ?? 1113) 
                : null;

            var httpEndpoint = new IPEndPoint(IPAddress.Parse(HttpEndpointAddress ?? SingleNodeAddress), HttpPort ?? 2113);

            ClusterSettings clusterSettings = null;
            //TODO
            //if (clusterSettingsBuilder != null)
            //{
            //    clusterSettings = clusterSettingsBuilder;
            //}
            //else
            //{
            //    clusterSettings = null;
            //}
            return new ConnectionConfiguration(connectionSettingsBuilder, clusterSettings, singleNodeAddress, httpEndpoint, Name);
        }
    }
}