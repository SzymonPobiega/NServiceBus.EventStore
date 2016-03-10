using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using EventStore.ClientAPI;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Unicast;

namespace NServiceBus.Transports.EventStore
{
    class Dispatcher : IDispatchMessages
    {
        IManageEventStoreConnections connectionManager;
        string outputQueue;

        public Dispatcher(IManageEventStoreConnections connectionManager, string endpointName)
        {
            this.connectionManager = connectionManager;
            this.outputQueue = "outputQueue-" + endpointName;
        }

        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            var unicastOps = outgoingMessages.UnicastTransportOperations.Select(ToEventData);
            var multicastOps = outgoingMessages.MulticastTransportOperations.Select(ToEventData);

            var allOps = unicastOps.Concat(multicastOps).ToArray();

            return connectionManager.GetConnection().AppendToStreamAsync(outputQueue, ExpectedVersion.Any, allOps);
        }

        static EventData ToEventData(UnicastTransportOperation operation)
        {
            var metadata = new EventStoreMessageMetadata()
            {
                DestinationQueue = operation.Destination,
            };
            return ToEventData(operation, metadata);
        }

        static EventData ToEventData(MulticastTransportOperation operation)
        {
            var metadata = new EventStoreMessageMetadata();
            return ToEventData(operation, metadata);
        }

        static EventData ToEventData(IOutgoingTransportOperation operation, EventStoreMessageMetadata metadata)
        {
            metadata.MessageId = operation.Message.MessageId;
            metadata.Headers = operation.Message.Headers;
            var type = operation.Message.Headers.ContainsKey(Headers.ControlMessageHeader) 
                ? "ControlMessage" 
                : operation.Message.Headers[Headers.EnclosedMessageTypes];

            byte[] data;
            string contentType;
            if (operation.Message.Headers.TryGetValue(Headers.ContentType, out contentType))
            {
                if (contentType != ContentTypes.Json)
                {
                    throw new InvalidOperationException("Invalid content type: "+contentType);
                }
                data = operation.Message.Body;
            }
            else
            {
                data = new byte[0];
            }
            return new EventData(Guid.NewGuid(), type, true, data, metadata.ToJsonBytes());
        }
    }
}