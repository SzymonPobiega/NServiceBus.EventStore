﻿namespace NServiceBus.Transports.EventStore
{
    public class TransactionalModeRouterProjectionCreator : AbstractProjectionCreator
    {
        private const string RouterProjectionQueryTemplate = @"fromStream('{0}')
.when({{
	$any: function (s, e) {{
		if (typeof e.metadata.destinationComponent !== 'undefined') {{
			linkTo('in_'+e.metadata.destinationComponent+'-{1}', e);
		}} else {{
			var atomicTypes = e.eventType.split(';');
		    for (var i = 0; i < atomicTypes.length; i++) {{
			    emit('events-'+atomicTypes[i]+'_{1}', atomicTypes[i], e.data, e.metadata);
		    }}
		}}		
	}}
}})";
        protected override string GetName(Address address)
        {
            return address.RouterProjectionName();
        }

        protected override string GetQuery(Address address)
        {
            return string.Format(RouterProjectionQueryTemplate,
                                             address.OutgoingStream(),
                                             address.GetComponentName());
        } 
    }
}