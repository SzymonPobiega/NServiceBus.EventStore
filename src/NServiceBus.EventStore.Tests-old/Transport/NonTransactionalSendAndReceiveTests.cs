using NUnit.Framework;

namespace NServiceBus.EventStore.Tests.Transport
{
    [TestFixture]
    public class NonTransactionalSendAndReceiveTests : TransportIntegrationTest
    {
        [Test]
        public void It_can_send_and_receive_messages()
        {
            var receiverAddress = GenerateAddress("receiver");
            var senderAddress = GenerateAddress("sender");
            var sender = CreateSender(senderAddress);

            var probe = new Probe(ConnectionConfiguration, receiverAddress);
            using (probe.ExpectReceived(5))
            {
                sender.SendMessages(senderAddress, receiverAddress, 5);
            }
        }
    }
}