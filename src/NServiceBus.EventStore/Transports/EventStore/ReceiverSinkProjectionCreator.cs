namespace NServiceBus.Transports.EventStore
{
    public class ReceiverSinkProjectionCreator : AbstractProjectionCreator
    {
        private const string RouterProjectionQueryTemplate = @"fromCategory('{0}')
.when({{
	$any: function (s, e) {{
		emit('{1}', e.eventType, e.data, e.metadata);
	}}
}})";
        protected override string GetName(Address address)
        {
            return address.GetReceiverSinkProjectionName();
        }

        protected override string GetQuery(Address address)
        {
            return string.Format(RouterProjectionQueryTemplate,
                                 address.GetReceiveStreamCategory(),
                                 address.GetFinalIncomingQueue());
        }
    }
}