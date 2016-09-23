using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Persistence;

namespace NServiceBus.EventSourcing
{
    public class Repository<T>
        where T : Aggregate, new()
    {
        public Repository(IMessageHandlerContext context, string collectionName)
        {
            if (!context.SynchronizedStorageSession.SupportsOutbox())
            {
                throw new Exception("Aggregates require enabling the Outbox feature.");
            }
            this.context = context;
            this.collectionName = collectionName;
        }

        public async Task<T> Load(Guid id)
        {
            var readResult = await context.SynchronizedStorageSession.ReadStreamEventsForwardAsync(BuildStreamName(id), 0, 4096, true).ConfigureAwait(false);
            if (readResult.Status == SliceReadStatus.StreamNotFound)
            {
                var newInstance = new T();
                newInstance.Hydrate(id, Enumerable.Empty<object>(), ExpectedVersion.NoStream);
                return newInstance;
            }
            if (readResult.Status == SliceReadStatus.StreamDeleted)
            {
                throw new Exception($"Aggregate {id} has been deleted.");
            }
            var lastEvent = readResult.Events.Last();
            if (lastEvent.Link != null && !lastEvent.IsResolved)
            {
                //We got an invalid link. This means the previous message has failed committing the outbox transaction. We throw here to trigger retries.
                throw new Exception("A previous message destined for this saga has failed. Triggering retries.");
            }
            var events = readResult.Events.Select(e => Deserialize(e.Event));
            var instance = new T();
            instance.Hydrate(id, events, readResult.LastEventNumber);
            return instance;
        }
        
        public async Task Store(T aggregate)
        {
            var events = aggregate.Dehydrate();
            var sendAction = aggregate.ProcessPorts().ToArray();
            var eventData = events.Select(Serialize);
            var streamName = BuildStreamName(aggregate.Id);

            foreach (var act in sendAction)
            {
                await act(context);
            }

            if (context.SynchronizedStorageSession.SupportsOutbox())
            {
                var links = eventData.Select(e => context.SynchronizedStorageSession.AtomicQueueForStore(streamName, e));
                await context.SynchronizedStorageSession.AppendToStreamAsync(streamName, aggregate.Version, links.ToArray()).ConfigureAwait(false);
            }
            else
            {
                await context.SynchronizedStorageSession.AppendToStreamAsync(streamName, aggregate.Version, eventData.ToArray()).ConfigureAwait(false);
            }
        }

        string BuildStreamName(Guid id)
        {
            return $"{collectionName}-{id}";
        }

        static object Deserialize(RecordedEvent e)
        {
            return e.Data.ParseJson(Type.GetType(e.EventType, true));
        }

        static EventData Serialize(object o)
        {
            return new EventData(Guid.NewGuid(), o.GetType().AssemblyQualifiedName, true, o.ToJsonBytes(), new byte[0]);
        }

        IMessageHandlerContext context;
        string collectionName;
    }
}