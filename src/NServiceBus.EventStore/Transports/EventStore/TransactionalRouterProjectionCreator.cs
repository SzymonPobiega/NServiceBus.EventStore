namespace NServiceBus.Transports.EventStore
{
    public class TransactionalRouterProjectionCreator : RouterProjectionCreator
    {
        protected override string GetName()
        {
            return "NSB_Router";
        }

        protected override string GetCategory()
        {
            return "outputQueue";
        }
    }
}