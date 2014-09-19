using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Unicast.Messages;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class PublishTest : SingleReceiverTest
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
            var publisher1Address = new Address("pub1", "node1");
            var publisher2Address = new Address("pub2", "node1");

            var projectionsManager = new ProjectionsManager(new NoopLogger(), HttpEndPoint, TimeSpan.FromSeconds(90));
            try
            {
                projectionsManager.EnableAsync("$by_category", AdminCredentials).Wait();
            }
            catch (Exception)
            {
                //best effort
            }

            var sinkProjectionCreator = new ReceiverSinkProjectionCreator
                {
                    ConnectionManager = new DefaultConnectionManager(ConnectionConfiguration)
                };
            sinkProjectionCreator.RegisterProjectionsFor(ReceiverAddress, "");

            var transactionalModeRouterProjectionCreator = new TransactionalModeRouterProjectionCreator()
            {
                ConnectionManager = new DefaultConnectionManager(ConnectionConfiguration)
            };
            transactionalModeRouterProjectionCreator.RegisterProjectionsFor(publisher1Address, "");
            transactionalModeRouterProjectionCreator.RegisterProjectionsFor(publisher2Address, "");

            var subscriptionManager = new SubscriptionManager(new DefaultConnectionManager(ConnectionConfiguration), MetadataRegistry)
                {
                    EndpointAddress = ReceiverAddress
                };

            var publisher1 = CreatePublisher(publisher1Address);
            var publisher2 = CreatePublisher(publisher2Address);

            subscriptionManager.Subscribe(typeof(EventA), publisher1.EndpointAddress);

            PublishMessages(publisher1, 1, typeof(EventA));

            if (!ExpectReceive(1, TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Received {0} messages out of 1", Count);
            }

            subscriptionManager.Subscribe(typeof(EventB), publisher2.EndpointAddress);

            PublishMessages(publisher2, 1, typeof(EventB));

            if (!ExpectReceive(1, TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Received {0} messages out of 1", Count);
            }

            subscriptionManager.Subscribe(typeof(EventC), publisher2.EndpointAddress);

            PublishMessages(publisher2, 1, typeof(EventC));

            if (!ExpectReceive(1, TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Received {0} messages out of 1", Count);
            }

            subscriptionManager.Unsubscribe(typeof(EventC), publisher2.EndpointAddress);

            PublishMessages(publisher2, 1, typeof(EventB));
            PublishMessages(publisher2, 1, typeof(EventC));

            if (!ExpectReceive(1, TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Received {0} messages out of 1", Count);
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