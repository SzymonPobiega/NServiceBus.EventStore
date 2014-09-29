namespace NServiceBus.Transports.EventStore
{
    public class EventSourcedRouterProjectionCreator : RouterProjectionCreator
    {
        protected override string GetName()
        {
            return "NSB_EventSourcedRouter";
        }

        protected override string GetCategory()
        {
            return "aggregates";
        }
    }
}