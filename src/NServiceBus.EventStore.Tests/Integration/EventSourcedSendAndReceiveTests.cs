using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class EventSourcedSendAndReceiveTests : SendAndReceiveTest
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
            sinkProjectionCreator.RegisterProjectionsFor(ReceiverAddress, "");

            var routerProjectionCreattor = new EventSourcedModeRouterProjectionCreator()
            {
                ConnectionManager = new DefaultConnectionManager(ConnectionConfiguration)
            };
            routerProjectionCreattor.RegisterProjectionsFor(SenderAddress, "");

            var unitOfWork = new EventSourcedUnitOfWork(new DefaultConnectionManager(ConnectionConfiguration))
                {
                    EndpointAddress = SenderAddress
                };
            var sender = CreateSender();
            
            unitOfWork.Begin();

            unitOfWork.Initialize("58",ExpectedVersion.NoStream);

            SendMessages(sender, 5);

            unitOfWork.End();

            if (!ExpectReceive(5, TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Received {0} messages out of 5", Count);
            }
        }
    }
}