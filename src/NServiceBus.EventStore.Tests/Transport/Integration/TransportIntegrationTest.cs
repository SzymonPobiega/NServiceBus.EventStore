using System;
using System.Linq;
using NServiceBus.EventStore.Tests;
using NServiceBus.Serializers.Json;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Serializers.Json;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class TransportIntegrationTest : IntegrationTest
    {
        protected MessageSender CreateSender(Address senderAddress)
        {
            var connectionManager = new DefaultConnectionManager(ConnectionConfiguration);
            var eventSourcedUnitOfWork = new EventSourcedUnitOfWork(connectionManager)
                {
                    EndpointAddress = senderAddress
                };
            return CreateSender(senderAddress, eventSourcedUnitOfWork);
        }

        protected MessageSender CreateSender(Address senderAddress, EventSourcedUnitOfWork eventSourcedUnitOfWork)
        {
            var connectionManager = new DefaultConnectionManager(ConnectionConfiguration);
            var transactionalUnitOfWork = new TransactionalUnitOfWork(connectionManager)
            {
                EndpointAddress = senderAddress
            };
            return new MessageSender(transactionalUnitOfWork, eventSourcedUnitOfWork, connectionManager)
            {
                EndpointAddress = senderAddress
            };
        }

        protected MessagePublisher CreatePublisher(Address sourceAddress)
        {
            var connectionManager = new DefaultConnectionManager(ConnectionConfiguration);
            var transactionalUnitOfWork = new TransactionalUnitOfWork(connectionManager)
                {
                    EndpointAddress = sourceAddress
                };
            var eventSourcedUnitOfWork = new EventSourcedUnitOfWork(connectionManager)
                {
                    EndpointAddress = sourceAddress
                };

            return new MessagePublisher(transactionalUnitOfWork, eventSourcedUnitOfWork, connectionManager)
                {
                    EndpointAddress = sourceAddress
                };
        }


        protected Address GenerateAddress(string suffix)
        {
            return new Address(GetType().Name + "_" + suffix + "_" + DateTime.Now.ToString("yy_MM_dd#HH_mm_ss"), null);
        }
    }

    public static class PublisherExtensions
    {
        public static void PublishEvents(this IPublishMessages publisher, Type eventType, int count, MessageMetadataRegistry metadataRegistry)
        {
            var definition = metadataRegistry.GetMessageMetadata(eventType);
            var enclosedMessageTypes = string.Join(";",definition.MessageHierarchy.Select(x => x.AssemblyQualifiedName));
            publisher.Publish(GenerateTransportMessage(i, enclosedMessageTypes), new PublishOptions(eventType));
            {
                publisher.Publish(MessageGenertor.GenerateTransportMessage(new Address("noreply",null), i, enclosedMessageTypes),
                    new[] {eventType});
            }
        }
    }

    public static class SenderExtensions
    {
        public static void SendMessages(this ISendMessages sender, Address source, Address destination, int count)
        {
            for (var i = 0; i < count; i++)
            {
                sender.Send(MessageGenertor.GenerateTransportMessage(source, i, "MessageType"), destination);
            }
        }
    }

    public static class MessageGenertor
    {
        public static TransportMessage GenerateTransportMessage(Address sender, int number, string messageTypes)
        {
            var message = new TransportMessage()
            {
                ReplyToAddress = sender,
                CorrelationId = "correlation",
                Body = JsonNoBomMessageSerializer.UTF8NoBom.GetBytes(string.Format("{{\"number\" : {0}}}", number))
            };
            message.Headers[Headers.EnclosedMessageTypes] = messageTypes;
            message.Headers[Headers.ContentType] = ContentTypes.Json;
            return message;
        }  
    }
}