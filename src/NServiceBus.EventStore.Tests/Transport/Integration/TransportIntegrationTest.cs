using System.Reflection;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class TransportIntegrationTest : IntegrationTest
    {
        protected Address ReceiverAddress = new Address("comp1", "store1");
        protected Address SenderAddress = new Address("comp2", "store1");
    }
}