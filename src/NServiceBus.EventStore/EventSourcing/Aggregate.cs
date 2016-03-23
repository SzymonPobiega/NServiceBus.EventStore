using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services.Description;

namespace NServiceBus.EventSourcing
{
    public abstract class Aggregate
    {
        readonly Port[] ports;
        protected internal Guid Id { get; private set; }
        internal int Version { get; private set; }
        internal bool Locked { get; private set; }

        protected Aggregate(params Port[] ports)
        {
            this.ports = ports;
        }

        internal void Hydrate(Guid id, IEnumerable<object> events, int version, bool locked)
        {
            Id = id;
            Version = version;
            Locked = locked;
            comittedEvents = events.ToList();
            foreach (var @event in comittedEvents)
            {
                ApplyEvent(@event);
            }
        }

        protected void Emit(object @event)
        {
            ApplyEvent(@event);
            uncomittedEvents.Add(@event);
        }

        void ApplyEvent(object @event)
        {
            ((dynamic) this).Apply((dynamic) @event);
        }

        internal IEnumerable<OutgoingMessage> ProcessPorts()
        {
            return ports.SelectMany(p => p.Process(Id, comittedEvents, uncomittedEvents));
        } 

        internal IEnumerable<object> Dehydrate()
        {
            return uncomittedEvents;
        }

        List<object> uncomittedEvents = new List<object>();
        List<object> comittedEvents = new List<object>();
    }
}