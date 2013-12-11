using NServiceBus.Transports.EventStore.Transactional;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    [TestFixture]
    public class TransactionalModeRouterProjectionCreatorTests : ProjectionCreatorTest<TransactionalModeRouterProjectionCreator>
    {        
        protected override string ProjectionName
        {
            get { return "comp1_router"; }
        }
    }
}