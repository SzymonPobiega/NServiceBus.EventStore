using NServiceBus.Features;

namespace NServiceBus
{
    class SagaPersisterFeature : Feature
    {
        public SagaPersisterFeature()
        {
            DependsOn<Features.Sagas>();
            DependsOn<SynchronizedStorageFeature>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}