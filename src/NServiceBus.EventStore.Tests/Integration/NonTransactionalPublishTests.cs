using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.NonTransactional;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class NonTransactionalPublishTests : SendAndReceiveTest
    {
        [Test]
        public void It_can_receive_subscribed_messages()
        {
            var projectionsManager = new ProjectionsManager(new NoopLogger(), HttpEndPoint);
            projectionsManager.Enable("$by_category", AdminCredentials);

            var sinkProjectionCreator = new ReceiverSinkProjectionCreator
                {
                    ConnectionManager = new DefaultConnectionManager(ConnectionConfiguration)
                };
            sinkProjectionCreator.RegisterProjectionsFor(ReceiverAddress, "");

            var subscriptionManager = new SubscriptionManager(new DefaultConnectionManager(ConnectionConfiguration))
                {
                    EndpointAddress = ReceiverAddress
                };
            
            var publisher1 =
                new NonTransactionalMessagePublisher(new DefaultConnectionManager(ConnectionConfiguration))
                    {
                        EndpointAddress = new Address("pub1", "node1")
                    };
            var publisher2 =
                new NonTransactionalMessagePublisher(new DefaultConnectionManager(ConnectionConfiguration))
                    {
                        EndpointAddress = new Address("pub2", "node1")
                    };

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