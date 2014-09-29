using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NServiceBus.Transports.EventStore.Config;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests
{
    public abstract class IntegrationTest
    {
        private const string EventStoreBinary = @"C:\Projects\EventStore\bin\ClusterNode\EventStore.ClusterNode.exe";
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
                eventStoreProcess = Process.Start(EventStoreBinary, "--run-projections=All --tcp-timeout=1000000 --mem-db");
                Thread.Sleep(5000);
            }
            TcpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113);
            HttpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113);
            ConnectionConfiguration = new ConnectionConfiguration(ConnectionSettings.Create().SetDefaultUserCredentials(AdminCredentials), null, TcpEndPoint, HttpEndPoint, "");
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