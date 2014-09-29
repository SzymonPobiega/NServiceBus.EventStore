using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Unicast.Messages;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class PolymorphicPublishTest : TransportIntegrationTest
    {
        private static readonly Address NotUsedAddress = null;
        protected MessageMetadataRegistry MetadataRegistry;

        protected abstract void PublishMessages(IPublishMessages publisher, int count, Type eventType);

        [SetUp]
        public void SetUpMessageMetadata()
        {
            MetadataRegistry = new MessageMetadataRegistry();
            MetadataRegistry.RegisterMessageType(typeof(IBaseEvent));
            MetadataRegistry.RegisterMessageType(typeof(IDerivedEvent1));
            MetadataRegistry.RegisterMessageType(typeof(IDerivedEvent2));
        }

        [Test]
        public void It_can_receive_subscribed_messages()
        {
            var publisher1Address = GenerateAddress("pub1");
            var publisher2Address = GenerateAddress("pub2");
            var genericSubscriberAddress = GenerateAddress("gen-sub");
            var specificSubscriberAddress = GenerateAddress("spec-sub");
            var genericSubscriberProbe = new Probe(ConnectionConfiguration, genericSubscriberAddress);
            var specificSubscriberProbe = new Probe(ConnectionConfiguration, specificSubscriberAddress);
            var metadataRegistry = new MessageMetadataRegistry();

            metadataRegistry.RegisterMessageType(typeof(IBaseEvent));
            metadataRegistry.RegisterMessageType(typeof(IDerivedEvent1));
            metadataRegistry.RegisterMessageType(typeof(IDerivedEvent2));

            var genericSubscriber = new SubscriptionManager(new DefaultConnectionManager(ConnectionConfiguration))
                {
                    EndpointAddress = genericSubscriberAddress
                };
            
            var specificSubscriber = new SubscriptionManager(new DefaultConnectionManager(ConnectionConfiguration))
                {
                    EndpointAddress = specificSubscriberAddress
                };

            var publisher1 = CreatePublisher(publisher1Address);
            var publisher2 = CreatePublisher(publisher2Address);

            genericSubscriber.Subscribe(typeof(IBaseEvent), NotUsedAddress);
            specificSubscriber.Subscribe(typeof(IDerivedEvent1), NotUsedAddress);

            using (specificSubscriberProbe.ExpectReceived(1))
            using (genericSubscriberProbe.ExpectReceived(2))
            {
                PublishMessages(publisher1, 1, typeof (IDerivedEvent1));
                PublishMessages(publisher2, 1, typeof (IDerivedEvent2));
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