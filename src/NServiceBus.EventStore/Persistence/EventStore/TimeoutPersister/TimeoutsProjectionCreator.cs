using NServiceBus.Internal;

namespace NServiceBus.Persistence.EventStore.TimeoutPersister
{
    class TimeoutsProjectionCreator : AbstractProjectionCreator
    {
        protected override string GetName()
        {
            return "NSB_Timeouts";
        }

        protected override string GetQuery()
        {
            return @"fromCategory('Timeouts')
.when({
    $init: function () {
        return {
            lastHeartbeatEpoch : null,
            firstTimeoutEpoch : null
        }; 
    },
    
	$any: function (s, e) {
	    if (e.eventType === '$heartbeat') {
	        if (s.lastHeartbeatEpoch === null) {
	            var startEpoch = s.firstTimeoutEpoch || e.data.epoch;
	            if (startEpoch > e.data.epoch) {
	                startEpoch = e.data.epoch;
	            }
	            emit('TimeoutsStart','$start',{ epoch : startEpoch});
	            
	            for (var i = startEpoch; i <= e.data.epoch; i++) {
	                emit('TimeoutsSlice-' + i, '$slice-end', {});
	            }
	        } else {
	            for (var i = s.lastHeartbeatEpoch + 1; i <= e.data.epoch; i++) {
	                emit('TimeoutsSlice-' + i, '$slice-end', {});
	            }
	        }
	        s.lastHeartbeatEpoch = e.data.epoch;
	    } else {
	        if (s.firstTimeoutEpoch === null || e.data.epoch < s.firstTimeoutEpoch) {
	           s.firstTimeoutEpoch = e.data.epoch;
	        }
	        var dueEpoch;
	        if (s.lastHeartbeatEpoch !== null && e.data.epoch <= s.lastHeartbeatEpoch) {
	            dueEpoch = s.lastHeartbeatEpoch + 1;
	        } else {
	            dueEpoch = e.data.epoch;
	        }
	        linkTo('TimeoutsSlice-' + dueEpoch, e, { 
	            dueTime : e.data.dueTime,
	            epoch : e.data.epoch
	        });
	    }
	}});";
        }
    }
}