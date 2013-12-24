using NServiceBus.Transports.EventStore;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class SagaIndexerProjectionCreator : AbstractProjectionCreator
    {
        protected override string GetQuery(Address address)
        {
            return @"fromCategory('SagaIntermediateIndex')
.foreachStream()
.when({
	$init: function () {
		return { 
			instanceId : null,				
		}; 
	},
	SagaData : function(s, e) {
		if (s.instanceId === null) {
			s.instanceId = e.data.id;
			linkTo(e.eventStreamId.replace('SagaIntermediateIndex-','SagaIndex-'), e);
		}
		if (s.instanceId === e.data.id) {				
			linkTo(e.eventStreamId.replace('SagaIntermediateIndex-','SagaIndex-'), e);
		}
	}
})";
        }

        protected override string GetName(Address address)
        {
            return "SagaIndex";
        }
    }
}