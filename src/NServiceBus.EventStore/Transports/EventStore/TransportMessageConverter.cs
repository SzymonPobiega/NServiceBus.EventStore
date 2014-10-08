using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Utils;
using System.Linq;
using NServiceBus.Internal;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Transports.EventStore
{
    public static class TransportMessageConverter
    {
        public static TransportMessage ToTransportMessage(this ResolvedEvent evnt)
        {
            if (evnt.Event.EventType.StartsWith("$"))
            {
                return null;
            }
            var metadata = evnt.Event.Metadata.ParseJson<EventStoreMessageMetadata>();
            var headers = metadata.Headers.ToDictionary(x => x.Key.ToPascalCase(), x => x.Value);
            if (metadata.ReplyTo != null)
            {
                headers[Headers.ReplyToAddress] = metadata.ReplyTo;
            }
            var transportMessage = new TransportMessage(metadata.MessageId, headers)
                {
                    Body = evnt.Event.Data,
                    CorrelationId = metadata.CorrelationId
                };
            return transportMessage;
        }

        

        public static EventData ToIndirectCommandEventData(this TransportMessage transportMessage, Address destination, Address replyToAddress)
        {
            var metadata = new EventStoreMessageMetadata()
                {
                    DestinationQueue = destination.Queue,
                    ReplyTo = replyToAddress != null ? replyToAddress.Queue : null
                };
            return ToEventData(transportMessage, metadata);
        }

        public static EventData ToEventEventData(this TransportMessage transportMessage, Address replyToAddress)
        {
            var metadata = new EventStoreMessageMetadata
                {
                    ReplyTo = replyToAddress != null ? replyToAddress.Queue : null
                };
            return ToEventData(transportMessage, metadata);
        }

        private static EventData ToEventData(TransportMessage transportMessage, EventStoreMessageMetadata metadata)
        {
            metadata.CorrelationId = transportMessage.CorrelationId;
            metadata.MessageId = transportMessage.Id;
            metadata.Headers = transportMessage.Headers;
            var type = transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader) 
                              ? "ControlMessage" 
                              : transportMessage.Headers[Headers.EnclosedMessageTypes];

            byte[] data;
            string contentType;
            if (transportMessage.Headers.TryGetValue(Headers.ContentType, out contentType))
            {
                if (contentType != ContentTypes.Json)
                {
                    throw new InvalidOperationException("Invalid content type: "+contentType);
                }
                data = transportMessage.Body;
            }
            else
            {
                data = new byte[0];
            }
            return new EventData(Guid.NewGuid(), type, true, data, metadata.ToJsonBytes());
        }
    }
}