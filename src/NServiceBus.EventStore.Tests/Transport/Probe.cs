using System;
using System.Threading;
using System.Transactions;
using NServiceBus.Internal;
using NServiceBus.Transports.EventStore;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;

namespace NServiceBus.EventStore.Tests.Transport
{
    public class ReceiveTask
    {
        private ManualResetEventSlim Event;
        private int actualCount;
        private readonly int targetCount;
        private readonly TimeSpan timeout;
        private DequeueStrategy dequeueStrategy;
        private readonly IConnectionConfiguration connectionConfiguration;
        private readonly Address address;

        public ReceiveTask(IConnectionConfiguration connectionConfiguration, Address address, int targetCount, TimeSpan timeout)
        {
            this.connectionConfiguration = connectionConfiguration;
            this.address = address;
            this.targetCount = targetCount;
            this.timeout = timeout;
        }

        public int TargetCount
        {
            get { return targetCount; }
        }

        public int ActualCount
        {
            get { return actualCount; }
        }

        public void Start()
        {
            dequeueStrategy = new DequeueStrategy(new DefaultConnectionManager(connectionConfiguration));
            Event = new ManualResetEventSlim();
            dequeueStrategy.Init(address, new TransactionSettings(true, TimeSpan.FromSeconds(60), IsolationLevel.Serializable, 0, false, false), 
                                 x =>
                                     {
                                         if (Interlocked.Increment(ref actualCount) == TargetCount)
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

        public bool Wait()
        {
            Event.Wait(timeout);
            dequeueStrategy.Stop();
            return ActualCount == TargetCount;
        }
    }

    public class ReceiverAssertion : IDisposable
    {
        private readonly ReceiveTask task;

        public ReceiverAssertion(ReceiveTask task)
        {
            this.task = task;
        }

        public void Dispose()
        {
            if (!task.Wait())
            {
                Assert.Fail("Expected {0} messages but received only {1}", task.TargetCount, task.ActualCount);
            }
        }
    }

    public class Probe
    {
        private readonly IConnectionConfiguration connectionConfiguration;
        private readonly Address address;

        public Probe(IConnectionConfiguration connectionConfiguration, Address address)
        {
            this.connectionConfiguration = connectionConfiguration;
            this.address = address;
        }

        public IDisposable ExpectReceived(int count)
        {
            return ExpectReceived(count, TimeSpan.FromSeconds(5));
        }

        public IDisposable ExpectReceived(int count, TimeSpan timeout)
        {
            var task = new ReceiveTask(connectionConfiguration, address, count, timeout);
            task.Start();
            return new ReceiverAssertion(task);
        }
    }
}