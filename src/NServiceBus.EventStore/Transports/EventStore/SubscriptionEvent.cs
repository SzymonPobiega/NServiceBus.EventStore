namespace NServiceBus.Transports.EventStore
{
    public class SubscriptionEvent
    {
        public string SubscriberEndpoint { get; set; }
        public string EventType { get; set; }
    }
}