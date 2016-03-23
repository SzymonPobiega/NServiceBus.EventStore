using System;
using NServiceBus.Internal;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.EventStore.Tests.Transport
{
    public abstract class PolymorphicPublishTest : TransportIntegrationTest
    {
        private static readonly Address NotUsedAddress = null;

        protected abstract void PublishMessages(IPublishMessages publisher, int count, Type eventType);

        [Test]
        public void It_can_receive_subscribed_messages()
        {
            var publisher1Address = GenerateAddress("pub1");
            var publisher2Address = GenerateAddress("pub2");
            var genericSubscriberAddress = GenerateAddress("gen-sub");
            var specificSubscriberAddress = GenerateAddress("spec-sub");
            var genericSubscriberProbe = new Probe(ConnectionConfiguration, genericSubscriberAddress);
            var specificSubscriberProbe = new Probe(ConnectionConfiguration, specificSubscriberAddress);

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