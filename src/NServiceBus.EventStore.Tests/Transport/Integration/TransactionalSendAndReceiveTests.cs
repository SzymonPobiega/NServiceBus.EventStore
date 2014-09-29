using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class TransactionalSendAndReceiveTests : TransportIntegrationTest
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
                using (var tx = new TransactionScope())
                {
                    sender.SendMessages(senderAddress, receiverAddress, 5);
                    tx.Complete();
                }
            }
        }
    }
}