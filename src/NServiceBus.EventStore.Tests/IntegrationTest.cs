using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NServiceBus.Internal;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests
{
    public abstract class IntegrationTest
    {
        private const string EventStoreBinary = @"..\..\..\..\lib\EventStore\ClusterNode\EventStore.ClusterNode.exe";
        private bool UseExternalEventStore = false;

        protected readonly UserCredentials AdminCredentials = new UserCredentials("admin", "changeit");

        protected ConnectionConfiguration ConnectionConfiguration;
        protected Process eventStoreProcess;
        protected string PathName;

        protected IPEndPoint TcpEndPoint;
        protected IPEndPoint HttpEndPoint;

        [SetUp]
        public void SetUp()
        {
            if (!UseExternalEventStore)
            {
                eventStoreProcess = Process.Start(EventStoreBinary, "--run-projections=All --mem-db");
                Thread.Sleep(5000);
            }
            TcpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113);
            HttpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113);

            ConnectionConfiguration = new ConnectionConfiguration(ConnectionSettings.Create().SetDefaultUserCredentials(AdminCredentials), null, TcpEndPoint, HttpEndPoint, "");


        }

        protected void CreateQueues(Address address)
        {
            using (var connectionManager = new DefaultConnectionManager(ConnectionConfiguration))
            {
                var projectionCreators = new AbstractProjectionCreator[]
                    {
                        new ReceiverSinkProjectionCreator
                            {
                                ConnectionManager = connectionManager
                            },
                        new TransactionalRouterProjectionCreator
                            {
                                ConnectionManager = connectionManager
                            },
                        new EventSourcedRouterProjectionCreator
                            {
                                ConnectionManager = connectionManager
                            },
                        new SubscriptionsProjectionCreator
                            {
                                ConnectionManager = connectionManager
                            },
                    };
                var queueCreator = new EventStoreQueueCreator(projectionCreators, connectionManager);
                queueCreator.CreateQueueIfNecessary(address, null);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (!UseExternalEventStore)
            {
                eventStoreProcess.Kill();
                eventStoreProcess.WaitForExit();
            }
        }
    }
}