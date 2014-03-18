using System;
using System.Threading;
using EventStore.ClientAPI.Common.Utils;
using EventStore.Common.Utils;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Config;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.AddIn.Tests.Integration
{
    public class Receiver
    {
        private ManualResetEventSlim Event;
        public int Count;
        private int targetCount;
        private DequeueStrategy dequeueStrategy;
        private readonly IConnectionConfiguration connectionConfiguration;
        private readonly Address address;

        public Receiver(IConnectionConfiguration connectionConfiguration, Address address)
        {
            this.connectionConfiguration = connectionConfiguration;
            this.address = address;
            this.Start();
        }

        private void Start()
        {
            dequeueStrategy = new DequeueStrategy(new DefaultConnectionManager(connectionConfiguration));
            Event = new ManualResetEventSlim();
            dequeueStrategy.Init(address, TransactionSettings.Default,
                                 x =>
                                     {
                                         x.Body.ParseJson<int>();
                                         if (Interlocked.Increment(ref Count) == targetCount)
                                         {
                                             Event.Set();
                                         }
                                         return true;
                                     },
                                 (m, e) =>
                                     {

                                     });
            dequeueStrategy.Start(1);
        }

        public void Reset()
        {
            Event.Reset();
            Count = 0;
        }

        public bool ExpectReceived(int messageNumber, TimeSpan timeout)
        {
            targetCount = messageNumber;
            Event.Wait(timeout);
            return Count == targetCount;
        }
    }
}