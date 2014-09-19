using System;
using System.Linq;
using NServiceBus.EventStore.Tests;
using NServiceBus.Serializers.Json;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Serializers.Json;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class SendAndReceiveTest : TransportIntegrationTest
    {
        protected MessageSender CreateSender()
        {
            var connectionManager = new DefaultConnectionManager(ConnectionConfiguration);
            var eventSourcedUnitOfWork = new EventSourcedUnitOfWork(connectionManager)
                {
                    EndpointAddress = SenderAddress
                };
            return CreateSender(eventSourcedUnitOfWork);
        }

        protected MessageSender CreateSender(EventSourcedUnitOfWork eventSourcedUnitOfWork)
        {
            var connectionManager = new DefaultConnectionManager(ConnectionConfiguration);
            var transactionalUnitOfWork = new TransactionalUnitOfWork(connectionManager)
            {
                EndpointAddress = SenderAddress
            };
            return new MessageSender(transactionalUnitOfWork, eventSourcedUnitOfWork, connectionManager)
            {
                EndpointAddress = SenderAddress
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


        protected void SendMessages(ISendMessages sender, int count)
        {
            for (var i = 0; i < count; i++)
            {
                sender.Send(GenerateTransportMessage(i, "MessageType"), ReceiverAddress);
            }
        }

        protected void PublishMessage(IPublishMessages publisher, Type eventType, int i, MessageMetadataRegistry metadataRegistry)
        {
            var definition = metadataRegistry.GetMessageDefinition(eventType);
            var enclosedMessageTypes = string.Join(";", definition.MessageHierarchy.Select(x => x.AssemblyQualifiedName));
            publisher.Publish(GenerateTransportMessage(i, enclosedMessageTypes), new[] { eventType });
        }

        private TransportMessage GenerateTransportMessage(int number, string messageTypes)
        {
            var message = new TransportMessage()
                {
                    ReplyToAddress = SenderAddress,
                    CorrelationId = "correlation",
                    Body = JsonNoBomMessageSerializer.UTF8NoBom.GetBytes(string.Format("{{\"number\" : {0}}}",number))
                };
            message.Headers[Headers.EnclosedMessageTypes] = messageTypes;
            message.Headers[Headers.ContentType] = ContentTypes.Json;
            return message;
        }  

    }
}