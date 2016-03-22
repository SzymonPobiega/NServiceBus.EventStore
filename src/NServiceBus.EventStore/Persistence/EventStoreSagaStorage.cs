using NServiceBus.Features;
using NServiceBus.Internal;

namespace NServiceBus
{
    class EventStoreSagaStorageFeature : Feature
    {
        public EventStoreSagaStorageFeature()
        {
            DependsOn<Features.Sagas>();
            DependsOn<EventStoreSynchronizedStorageFeature>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<EventStoreSagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}