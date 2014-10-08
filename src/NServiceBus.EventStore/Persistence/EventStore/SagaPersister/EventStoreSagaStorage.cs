using NServiceBus.Features;
using NServiceBus.Internal;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class EventStoreSagaStorage : Feature
    {
         /// <summary>
        /// Creates an instance of <see cref="EventStoreSagaStorage"/>.
        /// </summary>
        internal EventStoreSagaStorage()
        {
            DependsOn<Features.Sagas>();
            DependsOn<EventStoreConnectionManager>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<EventStoreSagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}