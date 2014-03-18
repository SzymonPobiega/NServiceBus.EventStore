using System;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class SingleReceiverTest : SendAndReceiveTest
    {
        private Receiver singleReceiver;
        protected int Count { get { return singleReceiver.Count; } }

        [SetUp]
        public void SendAndReceiveSetUp()
        {
            singleReceiver = new Receiver(ConnectionConfiguration, ReceiverAddress);
        }

        protected bool ExpectReceive(int messageNumber, TimeSpan timeout)
        {
            singleReceiver.Reset();
            return singleReceiver.ExpectReceived(messageNumber, timeout);
        }

        
    }
}