using System;
using System.IO;
using System.Net;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using EventStore.Core.Services;
using EventStore.Core.Tests.Helpers;
using EventStore.Projections.Core;
using NServiceBus.Transports.EventStore.Config;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests
{
    public abstract class IntegrationTest
    {
        private bool UseExternalEventStore = true;

        protected readonly UserCredentials AdminCredentials = new UserCredentials(SystemUsers.Admin, SystemUsers.DefaultAdminPassword);

        protected ConnectionConfiguration ConnectionConfiguration;
        protected MiniNode Node;
        protected string PathName;

        protected IPEndPoint TcpEndPoint;
        protected IPEndPoint HttpEndPoint;

        [SetUp]
        public void SetUp()
        {
            if (!UseExternalEventStore)
            {
                var projections = new ProjectionsSubsystem(1, RunProjections.All);
                Console.WriteLine("Usign data directory {0}",PathName);
                Node = new MiniNode(PathName, skipInitializeStandardUsersCheck: false, inMemDb:true, tcpPort:45060, httpPort:45062, subsystems: new ISubsystem[] { projections });
                Node.Start();
                TcpEndPoint = Node.TcpEndPoint;
                HttpEndPoint = Node.HttpEndPoint;
            }
            else
            {
                TcpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"),1113);
                HttpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"),2113);
            }
            ConnectionConfiguration = new ConnectionConfiguration(ConnectionSettings.Create().SetDefaultUserCredentials(AdminCredentials), null, TcpEndPoint, HttpEndPoint, "");
            Thread.Sleep(5000);
        }

        [TearDown]
        public void TearDown()
        {
            if (!UseExternalEventStore)
            {
                Console.WriteLine("Shutting down");
                Node.Shutdown();
            }
        }

        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            var typeName = GetType().Name.Length > 30 ? GetType().Name.Substring(0, 30) : GetType().Name;
            PathName = Path.Combine(Path.GetTempPath(), string.Format("{0}-{1}", Guid.NewGuid(), typeName));
            Directory.CreateDirectory(PathName);
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
        {
            //kill whole tree
            Directory.Delete(PathName, true);
        }
    }
}