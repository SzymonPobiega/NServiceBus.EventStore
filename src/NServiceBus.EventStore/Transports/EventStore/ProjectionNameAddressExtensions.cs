namespace NServiceBus.Transports.EventStore
{
    public static class ProjectionNameAddressExtensions
    {
        public static string ReceiverProjectionName(this Address address)
        {
            return address.Queue + "_receiver_sink";
        }

        public static string SubscriptionProjectionName(this Address address)
        {
            return address.Queue + "_subscriptions";
        }

        public static string RouterProjectionName(this Address address)
        {
            return address.Queue + "_router";
        }

        public static string EventSorucedRouterProjectionName(this Address address)
        {
            return address.Queue + "_es_router";
        }
    }
}