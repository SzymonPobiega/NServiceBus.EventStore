using NServiceBus.Internal;

namespace NServiceBus.Transports.EventStore
{
    class ReceiverSinkProjectionCreator : AbstractProjectionCreator
    {
        protected override string GetName()
        {
            return "NSB_Receiver";
        }

        protected override string GetQuery()
        {
            return @"fromCategory('inputs')
.when({
	$any: function (s, e) {
        if (e.eventType[0] !== '$') {
            var dest;
            if (typeof e.linkMetadata.destinationQueue !== 'undefined') {
                dest = e.linkMetadata.destinationQueue;
            } else {
                dest = e.metadata.destinationQueue;
            }
		    emit(dest, e.eventType, e.data, e.metadata);            
        }
	}
})";
        }
    }
}