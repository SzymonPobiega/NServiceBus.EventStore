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
    class OutboxPersister : IOutboxStorage, IDisposable
    {
        public const string DispatchedEventType = "$dispatched";
        public const string OutboxRecordEventType = "$outbox-record";
        IEventStoreConnection outboxConnection;

        public OutboxPersister(IConnectionConfiguration config)
        {
            if (config == null)
            {
                return;
            }
            outboxConnection = config.CreateConnection("Outbox");
            outboxConnection.ConnectAsync().GetAwaiter().GetResult();
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            var connection = GetConnection(context);
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
            var outboxRecord = DeserializeOutboxRecord(readResult);
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

        static OutboxRecordEvent DeserializeOutboxRecord(StreamEventsSlice readResult)
        {
            var outboxRecord = readResult.Events[0].Event.Data.ParseJson<OutboxRecordEvent>();
            return outboxRecord;
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var typedTransaction = (EventStoreOutboxTransaction) transaction;
            var persistenceOps = typedTransaction.Persist(message, GetStreamName(message.MessageId));
            context.Set(persistenceOps);
            return Task.FromResult(0);
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            var connection = GetConnection(context);
            var streamName = GetStreamName(messageId);

            OutboxPersistenceOperation[] persistenceOps;
            if (!context.TryGet(out persistenceOps))
            {
                return; //We can skip adding another dispatched event because if there is no persistence ops it means Get returned an alreadt dispatched outbox record.
            }
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
            var connection = GetConnection(context);
            return Task.FromResult<OutboxTransaction>(new EventStoreOutboxTransaction(connection));
        }

        IEventStoreConnection GetConnection(ContextBag context)
        {
            if (outboxConnection != null)
            {
                return outboxConnection;
            }
            var transportTransaction = context.Get<TransportTransaction>();
            IEventStoreConnection connection;
            if (!transportTransaction.TryGet(out connection))
            {
                throw new Exception("EventStore persistence can only be used either with EventStore transport or requires explicitly.");
            }
            return connection;
        }

        public void Dispose()
        {
            outboxConnection?.Dispose();
        }
    }
}