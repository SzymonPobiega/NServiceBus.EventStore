namespace NServiceBus.Transports.EventStore
{
    public static class AddressExtensions
    {
        public static string GetIntermediateOutgoingQueue(this Address address)
        {
            return address.Queue + "_intermediate_out";
        }

        public static string GetAggregateStreamCategory(this Address address)
        {
            return "ag_" + address.Queue;
        }

        public static string GetAggregateStream(this Address address, string aggregateId)
        {
            return "ag_" + address.Queue + "-" + aggregateId;
        }

        public static string GetComponentName(this Address address)
        {
            return address.Queue;
        }

        public static string GetReceiveStreamCategory(this Address address)
        {
            return "in_" + address.Queue;
        }

        public static string GetReceiveAddressFrom(this Address address, Address sourceAddress)
        {
            return "in_" + address.Queue + "-" + sourceAddress.Queue;
        }

        public static string GetFinalOutgoingQueue(this Address address)
        {
            return address.Queue + "_out";
        }

        public static string GetFinalIncomingQueue(this Address address)
        {
            return address.Queue + "_in";
        }

        public static string GetReceiverSinkProjectionName(this Address address)
        {
            return address.Queue + "_receiver_sink";
        }

        public static string GetSubscriptionProjectionName(this Address address)
        {
            return address.Queue + "_subscriptions";
        }

        public static string GetRouterProjectionName(this Address address)
        {
            return address.Queue + "_router";
        }

        public static string GetEventSorucedRouterProjectionName(this Address address)
        {
            return address.Queue + "_es_router";
        }
    }
}