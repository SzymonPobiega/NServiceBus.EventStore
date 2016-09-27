using System.Net;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using NUnit.Framework;

namespace NServiceBus.EventStore.Tests
{
    [SetUpFixture]
    class EmbeddedEventStoreManager
    {
        internal static ClusterVNode EmbeddedEventStore;
        static readonly object lockObject = new object();

        [OneTimeSetUp]
        public void CreateEventStore()
        {
            lock (lockObject)
            {
                if (EmbeddedEventStore == null)
                {
                    var nodeBuilder = EmbeddedVNodeBuilder.AsSingleNode()
                        .WithExternalTcpOn(new IPEndPoint(IPAddress.Loopback, 1113))
                        .RunInMemory();

                    var node = nodeBuilder.Build();
                    node.StartAndWaitUntilReady().GetAwaiter().GetResult();
                    EmbeddedEventStore = node;
                }
            }
        }

        [OneTimeTearDown]
        public void StopEventStore()
        {
            EmbeddedEventStore?.Stop();
        }
    }
}