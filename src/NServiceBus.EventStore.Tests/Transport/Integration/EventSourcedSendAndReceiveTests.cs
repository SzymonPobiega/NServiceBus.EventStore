using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.Core.Tests.TransactionLog.Scavenging.Helpers;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Serializers.Json;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class EventSourcedSendAndReceiveTests : SendAndReceiveTest
    {
        [Test]
        public void It_can_send_and_receive_messages()
        {
            var projectionsManager = new ProjectionsManager(new NoopLogger(), HttpEndPoint, TimeSpan.FromSeconds(90));
            projectionsManager.EnableAsync("$by_category", AdminCredentials).Wait();

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
            var sender = CreateSender(unitOfWork);
            
            unitOfWork.Begin();

            unitOfWork.Initialize("58",ExpectedVersion.NoStream);

            var receiver = new Receiver(ConnectionConfiguration, ReceiverAddress);

            //var conn = new DefaultConnectionManager(ConnectionConfiguration);
            //for (int i = 0; i < 7; i++)
            //{
            //    var data = JsonNoBomMessageSerializer.UTF8NoBom.GetBytes(string.Format("{{\"number\" : {0}}}", i));
            //    var meta = JsonNoBomMessageSerializer.UTF8NoBom.GetBytes("{\"destinationComponent\" : \"comp1\", \"headers\": {}, \"replyTo\" : \"comp2\"}");
            //    conn.GetConnection().AppendToStreamAsync("ag_comp2-58", ExpectedVersion.Any,
            //        new EventData(Guid.NewGuid(),"Message",true,data, meta)).Wait();
            //}
            SendMessages(sender, 5);

            unitOfWork.End();

            if (!receiver.ExpectReceived(5, TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Received {0} messages out of 5", receiver.Count);
            }
        }
    }
}