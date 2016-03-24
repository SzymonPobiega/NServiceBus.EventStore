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
        const string LockEventType = "$lock";

        public Repository(IMessageHandlerContext context, string collectionName)
        {
            if (!context.SynchronizedStorageSession.SupportsAtomicQueueForStore())
            {
                throw new Exception("Aggregates require enabling the Outbox feature.");
            }
            this.context = context;
            this.collectionName = collectionName;
        }

        public async Task<T> Load(Guid id)
        {
            var readResult = await context.SynchronizedStorageSession.ReadStreamEventsForwardAsync(BuildStreamName(id), 0, 4096, false).ConfigureAwait(false);
            if (readResult.Status == SliceReadStatus.StreamNotFound)
            {
                var newInstance = new T();
                newInstance.Hydrate(id, Enumerable.Empty<object>(), ExpectedVersion.NoStream, false);
                return newInstance;
            }
            if (readResult.Status == SliceReadStatus.StreamDeleted)
            {
                throw new Exception($"Aggregate {id} has been deleted.");
            }
            var locked = CheckLock(id, readResult);
            var events = readResult.Events.Where(e => e.Event.EventType != LockEventType).Select(e => Deserialize(e.Event));
            var instance = new T();
            instance.Hydrate(id, events, readResult.LastEventNumber, locked);
            return instance;
        }

        bool CheckLock(Guid id, StreamEventsSlice readResult)
        {
            var lastLock = readResult.Events.Last();
            if (lastLock.Event.EventType != LockEventType)
            {
                return false;
            }
            var lastLockData = lastLock.Event.Data.ParseJson<AggregateLockEvent>();
            if (lastLockData.LockedBy != context.MessageId)
            {
                throw new Exception($"Aggregate {id} locked for processing by message {lastLockData.LockedBy}.");
            }
            return true;
        }

        public async Task Store(T aggregate)
        {
            var events = aggregate.Dehydrate();
            var sendAction = aggregate.ProcessPorts().ToArray();
            var eventData = events.Select(Serialize);
            var streamName = BuildStreamName(aggregate.Id);

            if (sendAction.Any())
            {
                //Use the lock & outbox
                var lockEvent = new EventData(Guid.NewGuid(), LockEventType, true, new AggregateLockEvent(context.MessageId).ToJsonBytes(), new byte[0]);
                await context.SynchronizedStorageSession.AppendToStreamAsync(streamName, aggregate.Version, lockEvent).ConfigureAwait(false);

                foreach (var action in sendAction)
                {
                    await action(context).ConfigureAwait(false);
                }
                foreach (var e in eventData)
                {
                    context.SynchronizedStorageSession.AtomicQueueForStore(streamName, e);
                }
            }
            else
            {
                //Append events directly
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