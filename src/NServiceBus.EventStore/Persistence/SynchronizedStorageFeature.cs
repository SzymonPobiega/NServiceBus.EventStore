using NServiceBus.Features;

namespace NServiceBus
{
    class SynchronizedStorageFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SynchronizedStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}