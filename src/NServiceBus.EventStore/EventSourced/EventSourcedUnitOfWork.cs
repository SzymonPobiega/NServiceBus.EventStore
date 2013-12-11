using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI;
using NServiceBus.UnitOfWork;

namespace NServiceBus.Transports.EventStore.EventSourced
{
    public class EventSourcedUnitOfWork : IManageUnitsOfWork
    {
        public Address EndpointAddress { get; set; }

        private readonly IManageEventStoreConnections connectionManager;
        private string aggregateId;
        private int? expectedVersion;

        public EventSourcedUnitOfWork(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public void Begin()
        {
            messages = new List<EventData>();
        }

        public void End(Exception ex = null)
        {
            if (ex == null)
            {
                CommitTransaction();
            }
            messages = null;
        }

        public void SetAggregateId(string id)
        {
            aggregateId = id;
        }

        public void SetExpectedVersion(int aExpectedVersion)
        {
            expectedVersion = aExpectedVersion;
        }

        public void Enqueue(EventData message)
        {
            messages.Add(message);
        }

        private void CommitTransaction()
        {
            if (messages.Count == 0)
            {
                return;
            }

            if (aggregateId == null)
            {
                throw new InvalidOperationException("Stream ID must be set before completing the UoW.");
            }
            if (!expectedVersion.HasValue)
            {
                throw new InvalidOperationException("Expected version must be set before completing the UoW.");
            }

            if (!messages.Any())
                return;

            connectionManager.GetConnection().AppendToStream(EndpointAddress.GetAggregateStream(aggregateId), expectedVersion.Value, messages);
        }

        IList<EventData> messages;
    }

    
}