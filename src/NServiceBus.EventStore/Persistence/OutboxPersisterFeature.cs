using System;
using NServiceBus.Features;

namespace NServiceBus
{
    class OutboxPersisterFeature : Feature
    {
        public OutboxPersisterFeature()
        {
            DependsOn<Features.Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}