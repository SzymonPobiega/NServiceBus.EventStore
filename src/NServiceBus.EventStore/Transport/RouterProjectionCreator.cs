using NServiceBus.Internal;

namespace NServiceBus
{
    class RouterProjectionCreator : AbstractProjectionCreator
    {
        
        protected override string GetQuery()
        {
            return @"fromCategory('outputQueue')
.when({
	$any: function (s, e) {
		if (typeof e.metadata.destinationQueue !== 'undefined') {
            var dest = e.metadata.destinationQueue;
			linkTo('inputs-' + dest + '_commands', e);
		} else {
			var atomicTypes = e.eventType.split(';');
		    for (var i = 0; i < atomicTypes.length; i++) {
			    emit('events-' + atomicTypes[i], atomicTypes[i], e.data, e.metadata);
		    }
		}		
	}
})";
        }

        protected override string GetName()
        {
            return "NSB_Router";
        }
    }
}