namespace NServiceBus.Transports.EventStore.EventSourced
{
    public class EventSourcedModeRouterProjectionCreator : AbstractProjectionCreator
    {
        private const string RouterProjectionQueryTemplate = @"fromCategory('{0}')
.when({{
	$any: function (s, e) {{
		if (typeof e.metadata.destinationComponent !== 'undefined') {{
			linkTo('in_'+e.metadata.destinationComponent+'-{1}', e);
		}} else {{
			emit('{2}', e.eventType, e.data, e.metadata);
		}}		
	}}
}})";
        protected override string GetName(Address address)
        {
            return address.GetEventSorucedRouterProjectionName();
        }

        protected override string GetQuery(Address address)
        {
            return string.Format(RouterProjectionQueryTemplate,
                                 address.GetAggregateStreamCategory(),
                                 address.GetComponentName(),
                                 address.GetFinalOutgoingQueue());
        }
    }
}