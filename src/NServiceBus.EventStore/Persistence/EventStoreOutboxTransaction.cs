using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Outbox;

namespace NServiceBus
{
    class EventStoreOutboxTransaction : OutboxTransaction
    {
        public IEventStoreConnection Connection { get; }

        public EventStoreOutboxTransaction(IEventStoreConnection connection)
        {
            this.Connection = connection;
        }

        public void Dispose()
        {
            //NOOP
        }

        public Task Commit()
        {
            var record = new OutboxRecordEvent()
            {
                TransportOperations = transportOperations,
                PersistenceOperations = persistenceOperations.ToDictionary(o => o.Event.EventId, o => o.DestinationStream)
            };

            var events = new List<EventData>();
            events.Add(new EventData(Guid.NewGuid(), OutboxPersister.OutboxRecordEventType, true, record.ToJsonBytes(), new byte[0]));
            events.AddRange(persistenceOperations.Select(operation => operation.Event));
            return Connection.AppendToStreamAsync(outboxStream, ExpectedVersion.NoStream, events);
        }

        public void AddPersistenceOperation(string destinationStream, EventData eventData)
        {
            persistenceOperations.Add(new OutboxPersistenceOperation(destinationStream, eventData));
        }

        public OutboxPersistenceOperation[] Persist(OutboxMessage outboxMessage, string outboxStream)
        {
            transportOperations = outboxMessage.TransportOperations.ToArray();
            this.outboxStream = outboxStream;
            return persistenceOperations.ToArray();
        }

        string outboxStream;
        List<OutboxPersistenceOperation> persistenceOperations = new List<OutboxPersistenceOperation>();
        TransportOperation[] transportOperations;
    }
}