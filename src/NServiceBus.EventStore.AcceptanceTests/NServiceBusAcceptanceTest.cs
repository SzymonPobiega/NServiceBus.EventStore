using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.EventStore.AcceptanceTests.Helper.Interface;
using NServiceBus.Hosting.Helpers;
using NUnit.Framework;

namespace NServiceBus.AcceptanceTests
{
    /// <summary>
    /// Base class for all the NSB test that sets up our conventions
    /// </summary>
    [TestFixture]
    public abstract partial class NServiceBusAcceptanceTest
    {
        private bool UseExternalEventStore = false;

        protected IPEndPoint TcpEndPoint;
        protected IPEndPoint HttpEndPoint;
        protected IEventStoreController Controller;
        protected AppDomain EventStoreDomain;

        [SetUp]
        public void SetUpEventStore()
        {
            var loopback = IPAddress.Parse("127.0.0.1");
            if (!UseExternalEventStore)
            {                            
                EventStoreDomain = AppDomain.CreateDomain("EventStore", null, new AppDomainSetup()
                {
                    
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
                });
                Controller =
                    (IEventStoreController)
                    EventStoreDomain.CreateInstanceAndUnwrap("NServiceBus.EventStore.AcceptanceTests.Helper",
                                                             "NServiceBus.EventStore.AcceptanceTests.Helper.EventStoreController");

                TcpEndPoint = new IPEndPoint(loopback, 45070);
                HttpEndPoint = new IPEndPoint(loopback, 45072);
                Controller.Start(TcpEndPoint.Port, HttpEndPoint.Port);
            }
            else
            {
                TcpEndPoint = new IPEndPoint(loopback, 1113);
                HttpEndPoint = new IPEndPoint(loopback, 2113);                
            }
            Thread.Sleep(3000);

            var connectionString = string.Format("singleNode={0};port={1};httpAddress={2};httpPort={3};user=admin;password=changeit", 
                TcpEndPoint.Address, TcpEndPoint.Port,
                HttpEndPoint.Address, HttpEndPoint.Port);
            Environment.SetEnvironmentVariable("EventStoreTransportDefinition.ConnectionString", connectionString);

            var exclusionsField = typeof (AssemblyScanner).GetField("DefaultAssemblyExclusions", BindingFlags.Static | BindingFlags.NonPublic);
            var exclusions = ((string[]) exclusionsField.GetValue(null)).ToList();
            exclusions.Add("EventStore.");
            exclusionsField.SetValue(null, exclusions.ToArray());
        }

        [TearDown]
        public void TearDownEventStore()
        {
            if (!UseExternalEventStore)
            {
                Controller.Shutdown();
                AppDomain.Unload(EventStoreDomain);
                EventStoreDomain = null;
            }
        }

        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            //if (!UseExternalEventStore)
            //{
            //    EventStoreDomain = AppDomain.CreateDomain("EventStore", null, new AppDomainSetup()
            //    {
            //        ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
            //    });                
            //    Controller =
            //        (IEventStoreController)
            //        EventStoreDomain.CreateInstanceAndUnwrap("NServiceBus.EventStore.AcceptanceTests.Helper",
            //                                                 "NServiceBus.EventStore.AcceptanceTests.Helper.EventStoreController");
            //}
            //var typeName = GetType().Name.Length > 30 ? GetType().Name.Substring(0, 30) : GetType().Name;
            //PathName = Path.Combine(Path.GetTempPath(), string.Format("{0}-{1}", Guid.NewGuid(), typeName));
            //Directory.CreateDirectory(PathName);
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
        {
            //kill whole tree
            //Directory.Delete(PathName, true);
        }
    }
}