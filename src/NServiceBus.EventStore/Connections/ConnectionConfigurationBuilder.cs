using System.Net;
using EventStore.ClientAPI;

namespace NServiceBus.Internal
{
    public class ConnectionConfigurationBuilder
    {
        ConnectionSettingsBuilder connectionSettingsBuilder = EventStore.ClientAPI.ConnectionSettings.Create();
        ClusterSettingsBuilder clusterSettingsBuilder;

        public ConnectionSettingsBuilder ConnectionSettings => connectionSettingsBuilder;

        public ClusterSettingsBuilder ClusterSettings
        {
            get {
                return clusterSettingsBuilder ??
                       (clusterSettingsBuilder = EventStore.ClientAPI.ClusterSettings.Create());
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

            return new ConnectionConfiguration(connectionSettingsBuilder, null, singleNodeAddress);
        }
    }
}