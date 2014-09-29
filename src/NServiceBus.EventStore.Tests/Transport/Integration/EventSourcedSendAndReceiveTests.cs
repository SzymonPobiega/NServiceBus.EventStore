using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
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
            var unitOfWork = new EventSourcedUnitOfWork(new DefaultConnectionManager(ConnectionConfiguration))
                {
                    EndpointAddress = SenderAddress
                };
            var sender = CreateSender(unitOfWork);

            unitOfWork.Begin();

            unitOfWork.Initialize("58", ExpectedVersion.Any);

            var receiver = new Receiver(ConnectionConfiguration, ReceiverAddress);

            //var conn = new DefaultConnectionManager(ConnectionConfiguration);
            //var data = JsonNoBomMessageSerializer.UTF8NoBom.GetBytes(string.Format("{{\"number\" : {0}}}", 0));
            //var meta = JsonNoBomMessageSerializer.UTF8NoBom.GetBytes("{\"destinationComponent\" : \"comp1\", \"headers\": {}, \"replyTo\" : \"comp2\"}");
            //conn.GetConnection().AppendToStreamAsync("ag_comp2-58", ExpectedVersion.Any, new EventData(Guid.NewGuid(), "Message", true, data, meta)).Wait();
            SendMessages(sender, 5);

            unitOfWork.End();

            if (!receiver.ExpectReceived(5, TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Received {0} messages out of 5", receiver.Count);
            }
        }
    }
}