using NServiceBus.Features;
using NServiceBus.Internal;

namespace NServiceBus.Persistence.EventStore.TimeoutPersister
{
    public class EventStoreTimeoutStorage : Feature
    {
        internal EventStoreTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
            DependsOn<EventStoreConnectionManager>();
            RegisterStartupTask<EventStoreTimeoutReaderTask>();
            RegisterStartupTask<EventStoreTimeoutHeartbeatTask>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<EventStoreTimeoutPersister>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<TimeoutsProjectionCreator>(DependencyLifecycle.InstancePerCall);
        }
    }
}