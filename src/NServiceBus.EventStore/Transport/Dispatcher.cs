using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.DelayedDelivery;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Performance.TimeToBeReceived;
using NServiceBus.Transports;

namespace NServiceBus
{
    class Dispatcher : IDispatchMessages
    {        
        public Dispatcher(IConnectionConfiguration connectionConfig, SubscriptionManager subscriptionManager, TimeoutProcessor timeoutProcessor)
        {
            this.connectionConfig = connectionConfig;
            this.subscriptionManager = subscriptionManager;
            this.timeoutProcessor = timeoutProcessor;
        }

        public async Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            TransportTransaction transportTransaction;
            if (context.TryGet(out transportTransaction))
            {
                var connection = transportTransaction.Get<IEventStoreConnection>();
                await Dispatch(outgoingMessages, connection).ConfigureAwait(false);
            }
            else
            {
                using (var connection = connectionConfig.CreateConnection("Dispatch"))
                {
                    await connection.ConnectAsync().ConfigureAwait(false);
                    await Dispatch(outgoingMessages, connection).ConfigureAwait(false);
                }
            }
        }

        async Task Dispatch(TransportOperations outgoingMessages, IEventStoreConnection connection)
        {
            foreach (var op in outgoingMessages.UnicastTransportOperations)
            {
                DateTime? dueTime = null;
                var dueTimeConstraint = op.DeliveryConstraints.OfType<DoNotDeliverBefore>().FirstOrDefault();
                var deferConstraint = op.DeliveryConstraints.OfType<DelayDeliveryWith>().FirstOrDefault();
                if (dueTimeConstraint != null)
                {
                    dueTime = dueTimeConstraint.At;
                }
                else if (deferConstraint != null)
                {
                    dueTime = DateTime.UtcNow + deferConstraint.Delay;
                }
                if (dueTime != null)
                {
                    var meta = new MessageMetadata
                    {
                        Destination = op.Destination
                    };
                    var eventData = ToEventData(op, meta);
                    await timeoutProcessor.Defer(eventData, dueTime.Value, connection).ConfigureAwait(false);
                }
                else
                {
                    await connection.AppendToStreamAsync(op.Destination, ExpectedVersion.Any, ToEventData(op)).ConfigureAwait(false);
                }
            }
            foreach (var op in outgoingMessages.MulticastTransportOperations)
            {
                var destinations = await subscriptionManager.GetDestinationQueues(op.MessageType).ConfigureAwait(false);
                foreach (var destination in destinations)
                {
                    await connection.AppendToStreamAsync(destination, ExpectedVersion.Any, ToEventData(op)).ConfigureAwait(false);
                }
            }
        }

        static EventData ToEventData(IOutgoingTransportOperation operation, MessageMetadata meta = null)
        {
            var metadata = meta ?? new MessageMetadata();
            var timeToBeReceived = operation.DeliveryConstraints.OfType<DiscardIfNotReceivedBefore>().FirstOrDefault();
            if (timeToBeReceived != null && timeToBeReceived.MaxTime != TimeSpan.Zero)
            {
                metadata.TimeToBeReceived = DateTime.UtcNow + timeToBeReceived.MaxTime;
            }
            metadata.MessageId = operation.Message.MessageId;
            metadata.Headers = operation.Message.Headers;            
            var type = operation.Message.Headers.ContainsKey(Headers.ControlMessageHeader)
                ? "ControlMessage"
                : operation.Message.Headers[Headers.EnclosedMessageTypes];

            byte[] data;
            string contentType;
            if (operation.Message.Headers.TryGetValue(Headers.ContentType, out contentType))
            {
                data = contentType != ContentTypes.Json 
                    ? Encoding.UTF8.GetBytes(Convert.ToBase64String(operation.Message.Body)) 
                    : operation.Message.Body;
            }
            else
            {
                data = new byte[0];
                metadata.Empty = true;
            }
            return new EventData(Guid.NewGuid(), type, true, data, metadata.ToJsonBytes());
        }

        SubscriptionManager subscriptionManager;
        TimeoutProcessor timeoutProcessor;
        IConnectionConfiguration connectionConfig;
    }
}