using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using EventStore.ClientAPI;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Performance.TimeToBeReceived;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Unicast;

namespace NServiceBus
{
    class Dispatcher : IDispatchMessages
    {
        
        public Dispatcher(IConnectionConfiguration connectionConfig, string endpointName)
        {
            this.connectionConfig = connectionConfig;
            this.outputQueue = "outputQueue-" + endpointName;
        }

        public async Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            var unicastOps = outgoingMessages.UnicastTransportOperations.Select(ToEventData);
            var multicastOps = outgoingMessages.MulticastTransportOperations.Select(ToEventData);

            var allOps = unicastOps.Concat(multicastOps).ToArray();

            TransportTransaction transportTransaction;
            if (context.TryGet(out transportTransaction))
            {
                var connection = transportTransaction.Get<IEventStoreConnection>();
                await connection.AppendToStreamAsync(outputQueue, ExpectedVersion.Any, allOps);
            }
            else
            {
                using (var connection = connectionConfig.CreateConnection())
                {
                    await connection.ConnectAsync();
                    await connection.AppendToStreamAsync(outputQueue, ExpectedVersion.Any, allOps);
                }
            }
        }

        static EventData ToEventData(UnicastTransportOperation operation)
        {
            var metadata = new EventStoreMessageMetadata()
            {
                DestinationQueue = operation.Destination,
            };
            var timeToBeReceived = operation.DeliveryConstraints.OfType<DiscardIfNotReceivedBefore>().FirstOrDefault();
            if (timeToBeReceived != null && timeToBeReceived.MaxTime != TimeSpan.Zero)
            {
                metadata.TimeToBeReceived = DateTime.UtcNow + timeToBeReceived.MaxTime;
            }
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
                metadata.Empty = true;
            }
            return new EventData(Guid.NewGuid(), type, true, data, metadata.ToJsonBytes());
        }

        IConnectionConfiguration connectionConfig;
        string outputQueue;
    }
}