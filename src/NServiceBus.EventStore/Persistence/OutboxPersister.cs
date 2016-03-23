using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Outbox;
using NServiceBus.Transports;
using TransportOperation = NServiceBus.Outbox.TransportOperation;

namespace NServiceBus
{
    class OutboxPersister : IOutboxStorage
    {
        public const string DispatchedEventType = "$dispatched";
        public const string OutboxRecordEventType = "$outbox-record";

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            var connection = GetTransportConnection(context);
            var streamName = GetStreamName(messageId);

            var readResult = await connection.ReadStreamEventsForwardAsync(streamName, 0, 4096, false);
            if (readResult.Status != SliceReadStatus.Success)
            {
                return null;
            }
            if (readResult.Events.Last().Event.EventType == DispatchedEventType)
            {
                return new OutboxMessage(messageId, new TransportOperation[0]);
            }
            var outboxRecord = readResult.Events[0].Event.Data.ParseJson<OutboxRecordEvent>();
            var persistenceOps =
                readResult.Events.Where(
                    e => e.Event.EventType != DispatchedEventType && e.Event.EventType != OutboxRecordEventType)
                    .Select(
                        e =>
                        {
                            var r = e.Event;
                            var destination = outboxRecord.PersistenceOperations[r.EventId];
                            return new OutboxPersistenceOperation(destination, new EventData(r.EventId, r.EventType, r.IsJson, r.Data, r.Metadata));
                        })
                    .ToArray();

            context.Set(persistenceOps);
            return new OutboxMessage(messageId, outboxRecord.TransportOperations);
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var typedTransaction = (EventStoreOutboxTransaction) transaction;
            typedTransaction.Persist(message, GetStreamName(message.MessageId));
            return Task.FromResult(0);
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            var connection = GetTransportConnection(context);
            var streamName = GetStreamName(messageId);

            var persistenceOps = context.Get<OutboxPersistenceOperation[]>(); //we know that it should be there because Get is guaranteed to be called before.

            foreach (var op in persistenceOps)
            {
                await connection.AppendToStreamAsync(op.DestinationStream, ExpectedVersion.Any, op.Event).ConfigureAwait(false);
            }
            var dispatchedEvent = new EventData(Guid.NewGuid(),DispatchedEventType, false, new byte[0], new byte[0]);
            await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, dispatchedEvent).ConfigureAwait(false);
        }

        static string GetStreamName(string messageId)
        {
            return "nsb-outbox-" + messageId;
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var connection = GetTransportConnection(context);
            return Task.FromResult<OutboxTransaction>(new EventStoreOutboxTransaction(connection));
        }

        static IEventStoreConnection GetTransportConnection(ContextBag context)
        {
            var transportTransaction = context.Get<TransportTransaction>();
            IEventStoreConnection connection;
            if (!transportTransaction.TryGet(out connection))
            {
                throw new Exception("EventStore persistence can only be used with EventStore transport.");
            }
            return connection;
        }
    }
}