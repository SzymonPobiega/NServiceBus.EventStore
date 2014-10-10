using NServiceBus.Features;
using NServiceBus.Internal;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class EventStoreSagaStorage : Feature
    {
        internal EventStoreSagaStorage()
        {
            DependsOn<Features.Sagas>();
            DependsOn<EventStoreConnectionManager>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<EventStoreSagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}