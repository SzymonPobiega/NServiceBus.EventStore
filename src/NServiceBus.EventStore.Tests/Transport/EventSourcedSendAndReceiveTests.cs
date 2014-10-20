using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.EventStore.Tests.Transport
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