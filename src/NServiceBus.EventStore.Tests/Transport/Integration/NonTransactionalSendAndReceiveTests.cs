using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class NonTransactionalSendAndReceiveTests : SingleReceiverTest
    {
        [Test]
        public void It_can_send_and_receive_messages()
        {
            var nonTransactionalSender = CreateSender();

            SendMessages(nonTransactionalSender, 5);

            if (!ExpectReceive(5, TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Received {0} messages out of 5", Count);
            }
        }
    }
}