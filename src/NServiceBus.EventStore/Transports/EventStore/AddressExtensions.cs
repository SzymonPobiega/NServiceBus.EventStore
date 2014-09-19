namespace NServiceBus.Transports.EventStore
{
    public static class AddressExtensions
    {
        
        public static string GetAggregateStreamCategory(this Address address)
        {
            return "ag_" + address.Queue;
        }

        public static string GetAggregateStream(this Address address, string aggregateId)
        {
            return "ag_" + address.Queue + "-" + aggregateId;
        }

        public static string OutgoingStream(this Address address)
        {
            return address.Queue + "_intermediate_out";
        }

        public static string GetComponentName(this Address address)
        {
            return address.Queue;
        }

        public static string ReceiveStreamCategory(this Address address)
        {
            return "in_" + address.Queue;
        }

        public static string SubscriberReceiveStreamFrom(this Address address, Address sourceAddress)
        {
            if (address.Equals(sourceAddress))
            {
                return "in_" + address.Queue + "-local";
            }
            return ReceiveStreamCategory(address) + "-" + sourceAddress.Queue + "_direct";
        }

        public static string SubscriberReceiveStreamFrom(this Address address, string source)
        {
            return ReceiveStreamCategory(address) + "-" + source;
        }

        public static string IncomingQueue(this Address address)
        {
            return address.Queue + "_in";
        }
    }
}