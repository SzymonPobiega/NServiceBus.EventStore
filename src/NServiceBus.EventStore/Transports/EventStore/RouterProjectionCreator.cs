namespace NServiceBus.Transports.EventStore
{
    public abstract class RouterProjectionCreator : AbstractProjectionCreator
    {
        
        protected override string GetQuery()
        {
            return @"fromCategory('"+ GetCategory() +@"')
.when({
	$any: function (s, e) {
		if (typeof e.metadata.destinationQueue !== 'undefined') {
            var dest = e.metadata.destinationQueue;
			linkTo('inputQueue-' + dest + '_commands', e);
		} else {
			var atomicTypes = e.eventType.split(';');
		    for (var i = 0; i < atomicTypes.length; i++) {
			    emit('events-' + atomicTypes[i], atomicTypes[i], e.data, e.metadata);
		    }
		}		
	}
})";
        }

        protected abstract string GetCategory();
    }
}