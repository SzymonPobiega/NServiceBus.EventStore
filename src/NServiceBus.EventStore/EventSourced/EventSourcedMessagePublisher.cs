using System;
using System.Collections.Generic;

namespace NServiceBus.Transports.EventStore.EventSourced
{
    public class EventSourcedMessagePublisher : IPublishMessages
    {
        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            throw new InvalidOperationException("In EventSourced mode event publishing can only be done via an aggregate.");
        }
    }
}