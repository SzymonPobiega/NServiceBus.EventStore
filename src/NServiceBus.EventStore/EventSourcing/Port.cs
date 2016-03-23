using System;
using System.Collections.Generic;

namespace NServiceBus.EventSourcing
{
    public abstract class Port
    {
        protected Guid Id { get; private set; }

        internal IEnumerable<OutgoingMessage> Process(Guid id, IEnumerable<object> comittedEvents, IEnumerable<object> uncomittedEvents)
        {
            Id = id;
            foreach (var @event in comittedEvents)
            {
                ApplyEvent(@event);
            }
            reconstructing = false;
            foreach (var @event in uncomittedEvents)
            {
                ApplyEvent(@event);
            }
            return sentMessages;
        }

        protected void Send(object message, SendOptions sendOptions)
        {
            if (!reconstructing)
            {
                sentMessages.Add(new OutgoingMessage(message, sendOptions));
            }
        }

        void ApplyEvent(object @event)
        {
            ((dynamic)this).Apply((dynamic)@event);
        }

        bool reconstructing = true;
        List<OutgoingMessage> sentMessages = new List<OutgoingMessage>();
    }
}