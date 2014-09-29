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

        public static string GetOutgoingStream(this Address address)
        {
            return "outputQueue-" + address.Queue;
        }

        public static string GetDirectInputStream(this Address address)
        {
            return "inputQueue-" + address.Queue + "_direct";
        }

        public static string GetInputQueueStream(this Address address)
        {
            return address.Queue;
        }
    }
}