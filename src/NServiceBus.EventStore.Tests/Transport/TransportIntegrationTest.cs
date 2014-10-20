using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Internal;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Unicast;

namespace NServiceBus.EventStore.Tests.Transport
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
            CreateQueues(senderAddress);
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
            CreateQueues(sourceAddress);
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
        private static readonly Conventions conventions = new Conventions();

        public static void PublishEvents(this IPublishMessages publisher, Type eventType, int count)
        {
            var messageTypes = GetMessageTypes(eventType).Where(x => conventions.IsMessageType(x));
            var messageTypesString = string.Join(";", messageTypes.Select(x => x.AssemblyQualifiedName));

            for (var i = 0; i < count; i++ )
            {
                publisher.Publish(MessageGenertor.GenerateTransportMessage(new Address("noreply", null), i, messageTypesString),new PublishOptions(eventType));
            }
        }

        static IEnumerable<Type> GetMessageTypes(Type type)
        {
            yield return type;
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }

            var currentBaseType = type.BaseType;
            var objectType = typeof(Object);
            while (currentBaseType != null && currentBaseType != objectType)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }
    }

    public static class SenderExtensions
    {
        public static void SendMessages(this ISendMessages sender, Address source, Address destination, int count)
        {
            for (var i = 0; i < count; i++)
            {
                sender.Send(MessageGenertor.GenerateTransportMessage(source, i, "MessageType"), new SendOptions(destination));
            }
        }
    }

    public static class MessageGenertor
    {
        public static TransportMessage GenerateTransportMessage(Address sender, int number, string messageTypes)
        {
            var message = new TransportMessage()
            {
                CorrelationId = "correlation",
                Body = Json.UTF8NoBom.GetBytes(string.Format("{{\"number\" : {0}}}", number))
            };
            message.Headers[Headers.EnclosedMessageTypes] = messageTypes;
            message.Headers[Headers.ContentType] = ContentTypes.Json;
            message.Headers[Headers.ReplyToAddress] = sender.ToString();
            return message;
        }  
    }
}