using System;
using System.Transactions;
using NServiceBus.Transports;
using NUnit.Framework;

namespace NServiceBus.EventStore.Tests.Transport
{
    [TestFixture]
    public class TransactionalPublishTests : PublishTest
    {
        protected override void PublishMessages(IPublishMessages publisher, int count, Type eventType)
        {
            using (var tx = new TransactionScope())
            {
                publisher.PublishEvents(eventType, count);
                tx.Complete();
            }
        }
    }
}