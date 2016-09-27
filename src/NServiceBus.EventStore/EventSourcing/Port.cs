using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NServiceBus.EventSourcing
{
    public abstract class Port
    {
        protected Guid Id { get; private set; }

        internal IEnumerable<Func<IMessageHandlerContext, Task>> Process(Guid id, IEnumerable<object> comittedEvents, IEnumerable<object> uncomittedEvents)
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

        /// <summary>
        /// Sends a message via NServiceBus.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="sendOptions">Send options.</param>
        protected void Send(object message, SendOptions sendOptions = null)
        {
            if (!reconstructing)
            {
                sentMessages.Add(c => c.Send(message, sendOptions ?? new SendOptions()));
            }
        }

        protected void Publish(object evnt, PublishOptions publishOptions = null)
        {
            if (!reconstructing)
            {
                sentMessages.Add(c => c.Publish(evnt, publishOptions ?? new PublishOptions()));
            }
        }

        void ApplyEvent(object @event)
        {
            ((dynamic)this).Apply((dynamic)@event);
        }

        bool reconstructing = true;
        List<Func<IMessageHandlerContext, Task>> sentMessages = new List<Func<IMessageHandlerContext, Task>>();
    }
}