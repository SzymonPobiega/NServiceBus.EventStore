using System;
using NServiceBus.Features;
using NServiceBus.Internal;

namespace NServiceBus
{
    class OutboxPersisterFeature : Feature
    {
        public const string ConnectionConfigurationSettingsKey = "NServiceBus.EventStore.Persistence.ConnectionConfiguration";

        public OutboxPersisterFeature()
        {
            DependsOn<Features.Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            IConnectionConfiguration config;
            context.Settings.TryGet(ConnectionConfigurationSettingsKey, out config);

            context.Container.ConfigureComponent(_ => new OutboxPersister(config), DependencyLifecycle.SingleInstance);
        }
    }
}