using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using NServiceBus.Transports.EventStore.Serializers.Json;
using NServiceBus.UnitOfWork;

namespace NServiceBus.Transports.EventStore
{
    public class EventSourcedUnitOfWork : IManageUnitsOfWork, IEventSourcedUnitOfWork
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

        public void Initialize(string aggregateId, int expectedVersion)
        {
            if (aggregateId == null)
            {
                throw new ArgumentNullException("aggregateId");
            }
            this.aggregateId = aggregateId;
            this.expectedVersion = expectedVersion;
        }

        public bool IsInitialized
        {
            get { return aggregateId != null; }
        }

        public void Publish(params EventData[] rawMessages)
        {
            if (aggregateId == null || !expectedVersion.HasValue)
            {
                throw new InvalidOperationException("The unit of work has to be initialized prior to publishing events.");
            }
            messages.AddRange(rawMessages);
        }

        private void CommitTransaction()
        {
            if (messages.Count == 0)
            {
                return;
            }            
            connectionManager.GetConnection().AppendToStreamAsync(EndpointAddress.GetAggregateStream(aggregateId), expectedVersion.Value, messages).Wait();
        }

        List<EventData> messages;
        
    }

    
}