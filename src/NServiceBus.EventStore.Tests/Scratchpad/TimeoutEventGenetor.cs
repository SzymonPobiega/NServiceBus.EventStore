using System;
using System.Linq;
using System.Net;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Scratchpad
{
    [TestFixture]
    public class EventGenerator
    {
        [Test]
        public void GenerateAnEvent()
        {
            using (var conn = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113)))
            {
                conn.ConnectAsync().Wait();
                var metadata = new Metadata
                    {
                        FieldA = "A",
                        FieldB = "B"
                    };
                var payload = new Payload()
                    {
                        GuidValue = Guid.NewGuid(),
                        IntValue = 77,
                        StringValue = "SomeText"
                    };

                conn.AppendToStreamAsync("Stream", ExpectedVersion.Any, new EventData(Guid.NewGuid(), "Event", true,
                                                                                 payload.ToJsonBytes(),
                                                                                 metadata.ToJsonBytes())).Wait();
            }
        }

        public class Payload
        {
            public String StringValue { get; set; }
            public int IntValue { get; set; }
            public Guid GuidValue { get; set; }
        }

        public class Metadata
        {
            public string FieldA { get; set; }
            public string FieldB { get; set; }
        }
    }

    [TestFixture]
    public class TimeoutEventGenetor
    {       
        [Test]
        public void GenerateSomeTimeoutData()
        {
            var random = new Random();

            var sagaIds = Enumerable.Range(0, 10).Select(x => Guid.NewGuid()).ToArray();


            using (var conn = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113)))
            {
                conn.ConnectAsync().Wait();
                var timeouts = Enumerable.Range(0, 100)
                                         .Select(x => new TimeoutData()
                                             {
                                                 OwningTimeoutManager = "A",
                                                 Time = DateTime.Now.AddSeconds(random.Next(60*120))
                                             })
                                         .Select(x => x.ToJsonBytes())
                                         .Select(x =>
                                             new {
                                                 Stream = "Timeout-"+sagaIds[random.Next(10)].ToString("N"),
                                                 Data = new EventData(Guid.NewGuid(), "Timeout", true, x, new byte[0])
                                             });

                foreach (var timeout in timeouts)
                {
                    conn.AppendToStreamAsync(timeout.Stream, ExpectedVersion.Any, timeout.Data).Wait();
                }
            }
        }

        [Test]
        [Explicit]
        public void DeleteStream()
        {
            var streamId = "Timeout-f75ca537669a477a9eeac58e51d4a8be";
            using (var conn = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113)))
            {
                conn.ConnectAsync().Wait();
                conn.DeleteStreamAsync(streamId,ExpectedVersion.Any,true).Wait();
            }
        }
        
        [Test]
        [Explicit]
        public void ReadEvents()
        {
            var streamId = "TimeoutIndex-2013_11_12_7_41";
            using (var conn = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113)))
            {
                conn.ConnectAsync().Wait();
                var events = conn.ReadStreamEventsBackwardAsync(streamId, -1, int.MaxValue, true).Result;
                Console.WriteLine("");
            }
        }
    }

    public class TimeoutData
    {
        public DateTime Time { get; set; }
        public string OwningTimeoutManager { get; set; }
    }
}