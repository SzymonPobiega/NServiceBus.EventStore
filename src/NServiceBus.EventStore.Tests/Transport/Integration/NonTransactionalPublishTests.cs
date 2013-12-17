using System;
using NServiceBus.Transports;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class NonTransactionalPublishTests : PublishTest
    {
        protected override void PublishMessages(IPublishMessages publisher, int count, Type eventType)
        {
            for (var i = 0; i < count; i++)
            {
                PublishMessage(publisher, eventType, i);
            }
        }
    }
}