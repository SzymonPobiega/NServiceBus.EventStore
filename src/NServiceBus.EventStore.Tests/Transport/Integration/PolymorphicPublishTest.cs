using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Unicast.Messages;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class PolymorphicPublishTest : SendAndReceiveTest
    {
        private static readonly Address notUserAddress = null;

        protected abstract void PublishMessages(IPublishMessages publisher, int count, Type eventType);

        private Receiver genericSubscriberReceiver;
        private Receiver specificSubscriberReceiver;
        private Address publisher1Address;
        private Address publisher2Address;
        private Address genericSubscriberAddress;
        private Address specificSubscriberAddress;
        protected MessageMetadataRegistry MetadataRegistry;

        [SetUp]
        public void SetUpReceiversAndMessageMetadata()
        {
            publisher1Address = new Address("pub1", "node1");
            publisher2Address = new Address("pub2", "node1");
            genericSubscriberAddress = new Address("genSub", "store1");
            specificSubscriberAddress = new Address("specSub", "store1");

            genericSubscriberReceiver = new Receiver(ConnectionConfiguration, genericSubscriberAddress);
            specificSubscriberReceiver = new Receiver(ConnectionConfiguration, specificSubscriberAddress);

            MetadataRegistry = new MessageMetadataRegistry();
            MetadataRegistry.RegisterMessageType(typeof(IBaseEvent));
            MetadataRegistry.RegisterMessageType(typeof(IDerivedEvent1));
            MetadataRegistry.RegisterMessageType(typeof(IDerivedEvent2));
        }

        [Test]
        public void It_can_receive_subscribed_messages()
        {
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
            sinkProjectionCreator.RegisterProjectionsFor(genericSubscriberAddress, "");
            sinkProjectionCreator.RegisterProjectionsFor(specificSubscriberAddress, "");

            var transactionalModeRouterProjectionCreator = new TransactionalModeRouterProjectionCreator()
            {
                ConnectionManager = new DefaultConnectionManager(ConnectionConfiguration)
            };
            transactionalModeRouterProjectionCreator.RegisterProjectionsFor(publisher1Address, "");
            transactionalModeRouterProjectionCreator.RegisterProjectionsFor(publisher2Address, "");

            

            var genericSubscriber = new SubscriptionManager(new DefaultConnectionManager(ConnectionConfiguration), MetadataRegistry)
                {
                    EndpointAddress = genericSubscriberAddress
                };
            
            var specificSubscriber = new SubscriptionManager(new DefaultConnectionManager(ConnectionConfiguration), MetadataRegistry)
                {
                    EndpointAddress = specificSubscriberAddress
                };

            var publisher1 = CreatePublisher(publisher1Address);
            var publisher2 = CreatePublisher(publisher2Address);

            genericSubscriber.Subscribe(typeof(IBaseEvent), notUserAddress);
            specificSubscriber.Subscribe(typeof(IDerivedEvent1), notUserAddress);

            PublishMessages(publisher1, 5, typeof(IDerivedEvent1));
            PublishMessages(publisher2, 5, typeof(IDerivedEvent2));

            if (!specificSubscriberReceiver.ExpectReceived(5, TimeSpan.FromSeconds(10)))
            {
                Assert.Fail("Received {0} messages out of 1", specificSubscriberReceiver.Count);
            }
            if (!genericSubscriberReceiver.ExpectReceived(10, TimeSpan.FromSeconds(10)))
            {
                Assert.Fail("Received {0} messages out of 2", genericSubscriberReceiver.Count);
            }
            
        }


        public interface IBaseEvent : IEvent
        {
            int BaseField { get; set; }
        }

        public interface IDerivedEvent1 : IBaseEvent
        {
            string DerivedField1 { get; set; }
        }

        public interface IDerivedEvent2 : IBaseEvent
        {
            string DerivedField2 { get; set; }
        }
    }
}