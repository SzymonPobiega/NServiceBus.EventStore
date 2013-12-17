using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class EventSourcedModeRouterProjectionCreatorTests : ProjectionCreatorTest<EventSourcedModeRouterProjectionCreator>
    {        
        protected override string ProjectionName
        {
            get { return "comp1_es_router"; }
        }
    }
}