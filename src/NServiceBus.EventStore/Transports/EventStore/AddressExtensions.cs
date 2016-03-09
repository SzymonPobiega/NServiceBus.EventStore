namespace NServiceBus.Transports.EventStore
{
    public static class AddressExtensions
    {
        public static string GetOutgoingStream(this Address address)
        {
            return "outputQueue-" + address.Queue;
        }

        public static string GetInputQueueStream(this Address address)
        {
            return  address.Queue;
        }
    }
}