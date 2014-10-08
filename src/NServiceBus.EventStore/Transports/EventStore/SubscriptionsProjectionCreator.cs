using NServiceBus.Internal;

namespace NServiceBus.Transports.EventStore
{
    class SubscriptionsProjectionCreator : AbstractProjectionCreator
    {
        protected override string GetQuery()
        {
            return @"fromCategory('events')     
.when({
    $init: function () {
        return {}; 
    },
                        
    $any: function (s, e) {            
        var subscriberEndpoint;            
        var subscribers;
        if (e.eventType === '$subscribe') {
            subscribers = s[e.data.eventType] || (s[e.data.eventType] = []);
            subscriberEndpoint = e.data.subscriberEndpoint;
            if (subscribers.indexOf(subscriberEndpoint === -1)) {
                subscribers.push(subscriberEndpoint);
            }
            return s;
        } else if (e.eventType === '$unsubscribe') {
            subscribers = s[e.data.eventType] || (s[e.data.eventType] = []);
            subscriberEndpoint = e.data.subscriberEndpoint;                
            var index = subscribers.indexOf(subscriberEndpoint);
            if (index != -1) {
                subscribers.splice(index, 1);
            }                
            return s;
        } else {
            subscribers = s[e.eventType] || [];
            for (var i = 0; i < subscribers.length; i++) {
                e.metadata.destinationQueue = subscribers[i];
                emit('inputQueue-' + subscribers[i] + '_events', e.eventType, e.data, e.metadata);
                //linkTo('inputQueue-' + subscribers[i] + '_events', e, linkMeta);
            }
        }
    }
})";
        }

        protected override string GetName()
        {
            return "NSB_Subscriptions";
        }
    }
}