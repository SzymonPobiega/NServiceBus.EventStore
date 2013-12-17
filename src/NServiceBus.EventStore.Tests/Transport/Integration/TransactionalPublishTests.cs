using System;
using System.Transactions;
using NServiceBus.Transports;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class TransactionalPublishTests : PublishTest
    {
        protected override void PublishMessages(IPublishMessages publisher, int count, Type eventType)
        {
            using (var tx = new TransactionScope())
            {
                for (var i = 0; i < count; i++)
                {
                    PublishMessage(publisher, eventType, i);
                }
                tx.Complete();
            }
        }
    }
}