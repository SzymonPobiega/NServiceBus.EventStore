using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Utils;
using System.Linq;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Transports.EventStore
{
    public static class TransportMessageConverter
    {
        public static TransportMessage ToTransportMessage(this ResolvedEvent evnt)
        {
            var metadata = evnt.Event.Metadata.ParseJson<EventStoreMessageMetadata>();
            var headers = metadata.Headers.ToDictionary(x => x.Key.ToPascalCase(), x => x.Value);
            var transportMessage = new TransportMessage(metadata.MessageId, headers)
                {
                    Body = evnt.Event.Data,
                    ReplyToAddress = Address.Parse(metadata.ReplyTo),
                    CorrelationId = metadata.CorrelationId
                };
            return transportMessage;
        }

        

        public static EventData ToIndirectCommandEventData(this TransportMessage transportMessage, Address destination)
        {
            var metadata = new EventStoreMessageMetadata()
                {
                    DestinationComponent = destination.GetComponentName()
                };
            return ToEventData(transportMessage, metadata);
        }

        public static EventData ToEventEventData(this TransportMessage transportMessage, IEnumerable<Type> eventTypes)
        {
            return ToEventData(transportMessage, new EventStoreMessageMetadata());
        }
        
        public static EventData ToDirectCommandEventData(this TransportMessage transportMessage, Address destination)
        {
            return ToEventData(transportMessage, new EventStoreMessageMetadata());
        }

        private static EventData ToEventData(TransportMessage transportMessage, EventStoreMessageMetadata metadata)
        {
            metadata.CorrelationId = transportMessage.CorrelationId;
            metadata.MessageId = transportMessage.Id;
            metadata.ReplyTo = transportMessage.ReplyToAddress.ToString();
            metadata.Headers = transportMessage.Headers;
            var type = transportMessage.IsControlMessage() 
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