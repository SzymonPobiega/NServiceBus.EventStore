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
    public class EventSourcedSendAndReceiveTests : TransportIntegrationTest
    {
        [Test]
        public void It_can_send_and_receive_messages()
        {
            var senderAddress = GenerateAddress("sender");
            var receiverAddress = GenerateAddress("receiver");

            var unitOfWork = new EventSourcedUnitOfWork(new DefaultConnectionManager(ConnectionConfiguration))
                {
                    EndpointAddress = senderAddress
                };
            var sender = CreateSender(senderAddress, unitOfWork);

            unitOfWork.Begin();

            unitOfWork.Initialize("58", ExpectedVersion.Any);

            var probe = new Probe(ConnectionConfiguration, receiverAddress);


            using (probe.ExpectReceived(5))
            {
                sender.SendMessages(senderAddress, receiverAddress, 5);
                unitOfWork.End();
            }
        }
    }
}