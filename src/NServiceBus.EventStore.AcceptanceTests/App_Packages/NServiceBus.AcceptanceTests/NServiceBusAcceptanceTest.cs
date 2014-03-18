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

namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    /// <summary>
    /// Base class for all the NSB test that sets up our conventions
    /// </summary>
    [TestFixture]    
    public abstract class NServiceBusAcceptanceTest
    {
        private bool UseExternalEventStore = false;

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
                Node = new MiniNode(PathName, skipInitializeStandardUsersCheck: false, inMemDb: true, tcpPort:45070, httpPort:45072, subsystems: new ISubsystem[] { projections });
                Node.Start();
                TcpEndPoint = Node.TcpEndPoint;
                HttpEndPoint = Node.HttpEndPoint;
            }
            else
            {
                TcpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113);
                HttpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113);
                //TcpEndPoint = new IPEndPoint(IPAddress.Parse("10.0.1.4"), 1113);
                //HttpEndPoint = new IPEndPoint(IPAddress.Parse("10.0.1.4"), 2113);
            }
            Thread.Sleep(3000);

            var connectionString = string.Format("singleNode={0};port={1};httpAddress={2};httpPort={3};user=admin;password=changeit", 
                TcpEndPoint.Address, TcpEndPoint.Port,
                HttpEndPoint.Address, HttpEndPoint.Port);
            Environment.SetEnvironmentVariable("EventStore.ConnectionString", connectionString);
            Conventions.EndpointNamingConvention= t =>
                {
                    var baseNs = typeof (NServiceBusAcceptanceTest).Namespace;
                    var testName = GetType().Name;
                    return t.FullName.Replace(baseNs + ".", "").Replace(testName + "+", "");
                };

            Conventions.DefaultRunDescriptor = () => ScenarioDescriptors.Transports.Default;
        }

        [TearDown]
        public void TearDown()
        {
            if (!UseExternalEventStore)
            {
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