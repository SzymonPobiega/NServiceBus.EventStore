namespace NServiceBus.Transports.EventStore
{
    public class Topology
    {
        private readonly string endpointUniqueName;

        public Topology(Address address, bool useSingleBrokerQueue)
        {
            endpointUniqueName = useSingleBrokerQueue
                                     ? address.Queue
                                     : address.Queue + "@" + address.Machine;
        }

        public string EndpointUniqueName { get { return endpointUniqueName; } }
    }
}