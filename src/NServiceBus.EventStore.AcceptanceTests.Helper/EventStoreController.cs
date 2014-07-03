using System;
using System.IO;
using System.Net;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using EventStore.Core.Services;
using EventStore.Core.Tests.Helpers;
using EventStore.Projections.Core;
using NServiceBus.EventStore.AcceptanceTests.Helper.Interface;

namespace NServiceBus.EventStore.AcceptanceTests.Helper
{
    public class EventStoreController : MarshalByRefObject, IEventStoreController
    {
        protected readonly UserCredentials AdminCredentials = new UserCredentials(SystemUsers.Admin, SystemUsers.DefaultAdminPassword);

        protected MiniNode Node;

        protected IPEndPoint TcpEndPoint;
        protected IPEndPoint HttpEndPoint;

        public void Start(int tcpPort, int httpPort)
        {
            var typeName = GetType().Name.Length > 30 ? GetType().Name.Substring(0, 30) : GetType().Name;
            var pathName = Path.Combine(Path.GetTempPath(), string.Format("{0}-{1}", Guid.NewGuid(), typeName));
            Directory.CreateDirectory(pathName);

            var projections = new ProjectionsSubsystem(1, RunProjections.All);
            Node = new MiniNode(pathName, skipInitializeStandardUsersCheck: false, inMemDb: true, tcpPort: tcpPort, httpPort: httpPort, subsystems: new ISubsystem[] { projections });
            Node.Start();
            TcpEndPoint = Node.TcpEndPoint;
            HttpEndPoint = Node.HttpEndPoint;
        }

        public void Shutdown()
        {
            Node.Shutdown();
        }
    }
}