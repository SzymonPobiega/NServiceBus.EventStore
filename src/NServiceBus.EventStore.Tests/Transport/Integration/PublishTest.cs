using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Unicast.Messages;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class PublishTest : TransportIntegrationTest
    {
        protected MessageMetadataRegistry MetadataRegistry;
        protected abstract void PublishMessages(IPublishMessages publisher, int count, Type eventType);

        [SetUp]
        public void SetUpMessageMetadata()
        {
            MetadataRegistry = new MessageMetadataRegistry();
            MetadataRegistry.RegisterMessageType(typeof(EventA));
            MetadataRegistry.RegisterMessageType(typeof(EventB));
            MetadataRegistry.RegisterMessageType(typeof(EventC));
        }

        [Test]
        public void It_can_receive_subscribed_messages()
        {
            var subscriberAddress = GenerateAddress("sub");
            var receiver = new Probe(ConnectionConfiguration, subscriberAddress);

            var subscriptionManager = new SubscriptionManager(new DefaultConnectionManager(ConnectionConfiguration))
                {
                    EndpointAddress = subscriberAddress
                };

            var publisher1 = CreatePublisher(GenerateAddress("pub1"));
            var publisher2 = CreatePublisher(GenerateAddress("pub2"));

            subscriptionManager.Subscribe(typeof(EventA), publisher1.EndpointAddress);

            using (receiver.ExpectReceived(1))
            {
                PublishMessages(publisher1, 1, typeof(EventA));                
            }

            subscriptionManager.Subscribe(typeof(EventB), publisher2.EndpointAddress);

            using (receiver.ExpectReceived(1))
            {
                PublishMessages(publisher2, 1, typeof (EventB));
            }
            
            subscriptionManager.Subscribe(typeof(EventC), publisher2.EndpointAddress);

            using (receiver.ExpectReceived(1))
            {
                PublishMessages(publisher2, 1, typeof (EventC));
            }
            
            subscriptionManager.Unsubscribe(typeof(EventC), publisher2.EndpointAddress);
            using (receiver.ExpectReceived(1))
            {
                PublishMessages(publisher2, 1, typeof (EventB));
                PublishMessages(publisher2, 1, typeof (EventC));
            }
        }


        public class EventA
        {
        }

        public class EventB
        {
        }

        public class EventC
        {
        }
    }
}