using System;
using NServiceBus.Transports;
using NUnit.Framework;

namespace NServiceBus.EventStore.Tests.Transport
{
    [TestFixture]
    public class NonTransactionalPolymorphicPublishTests : PolymorphicPublishTest
    {
        protected override void PublishMessages(IPublishMessages publisher, int count, Type eventType)
        {
            publisher.PublishEvents(eventType, count);
        }
    }
}