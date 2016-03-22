using NServiceBus.Features;

namespace NServiceBus
{
    class EventStoreSynchronizedStorageFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<EventStoreSynchronizedStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}