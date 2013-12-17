using System;
using System.Transactions;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class TransactionalSendAndReceiveTests : SendAndReceiveTest
    {
        [Test]
        public void It_can_send_and_receive_messages()
        {
            var projectionsManager = new ProjectionsManager(new NoopLogger(), HttpEndPoint);
            projectionsManager.Enable("$by_category", AdminCredentials);

            var sinkProjectionCreator = new ReceiverSinkProjectionCreator
                {
                    ConnectionManager = new DefaultConnectionManager(ConnectionConfiguration)
                };
            sinkProjectionCreator.RegisterProjectionsFor(ReceiverAddress,"");

            var routerProjectionCreattor = new TransactionalModeRouterProjectionCreator()
                {
                    ConnectionManager = new DefaultConnectionManager(ConnectionConfiguration)
                };
            routerProjectionCreattor.RegisterProjectionsFor(SenderAddress,"");

            var transactionalSender = CreateSender();

            using (var tx = new TransactionScope())
            {
                SendMessages(transactionalSender, 5);
                tx.Complete();
            }

            if (!ExpectReceive(5, TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Received {0} messages out of 5", Count);
            }
        }
    }
}