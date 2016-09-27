using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.EventSourcing;
using NUnit.Framework;

namespace NServiceBus.EventStore.Tests
{
    [TestFixture]
    public class AggregateTest
    {
        [Test]
        public void It_applies_emitted_event()
        {
            var a = new MyAggregate();

            a.Do();

            Assert.IsTrue(a.EventApplied);
        }

        [Test]
        public void It_retains_emitted_events()
        {
            var a = new MyAggregate();

            a.Do();

            Assert.AreEqual(1, a.Dehydrate().Count());
        }

        [Test]
        public void It_runs_the_event_through_ports()
        {
            var a = new MyAggregate(new MyPort());

            a.Do();
            var messages = a.ProcessPorts();

            Assert.AreEqual(1, messages.Count());
        }

        public class MyAggregate : Aggregate
        {
            public MyAggregate(params Port[] ports) : base(ports)
            {
            }

            public void Do()
            {
                Emit(new MyEvent());
            }

            public void Apply(MyEvent e)
            {
                EventApplied = true;
            }

            public bool EventApplied { get; private set; }
        }

        public class MyPort : Port
        {
            public void Apply(MyEvent e)
            {
                EventApplied = true;
                Send(new MyMessage());
            }

            public bool EventApplied { get; private set; }
        }

        public class MyEvent
        {
        }

        public class MyMessage
        {
        }
    }
}
