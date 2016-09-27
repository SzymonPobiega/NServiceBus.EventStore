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

        public EventStoreOutboxTransaction(string outboxStream, string messageId, IEventStoreConnection connection)
        {
            this.Connection = connection;
            this.outboxStream = outboxStream;
            this.messageId = messageId;
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

        public EventData AddPersistenceOperation(string destinationStream, EventData eventData)
        {
            persistenceOperations.Add(new OutboxPersistenceOperation(destinationStream, eventData));
            var index = persistenceOperations.Count;
            var link = Json.UTF8NoBom.GetBytes($"{index}@{outboxStream}");
            var linkMetadata = new LinkEvent()
            {
                MessageId = messageId
            };
            return new EventData(Guid.NewGuid(), "$>", false, link, linkMetadata.ToJsonBytes());
        }

        public void Persist(OutboxMessage outboxMessage)
        {
            transportOperations = outboxMessage.TransportOperations.ToArray();
        }

        string outboxStream;
        string messageId;
        List<OutboxPersistenceOperation> persistenceOperations = new List<OutboxPersistenceOperation>();
        TransportOperation[] transportOperations;
    }
}