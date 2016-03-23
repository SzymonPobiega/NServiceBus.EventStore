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
        public void It_applies_an_event()
        {
            var a = new MyAggregate();
            a.Do();
            var events = a.Dehydrate();
        }

        public class MyAggregate : Aggregate
        {
            public MyAggregate() : base()
            {
            }

            public void Do()
            {
                Emit(new MyEvent());
            }

            public void Apply(MyEvent e)
            {
            }
        }

        public class MyEvent
        {
        }
    }
}
