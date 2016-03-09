using NServiceBus.Internal;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;

namespace NServiceBus.Features
{
    class EventStoreTransportFeature : ConfigureTransport
    {
        public EventStoreTransportFeature()
        {
            DependsOn<EventStoreConnectionManager>();
        }

        protected override void Configure(FeatureConfigurationContext context, string connectionString)
        {            
            var localAddress = context.Settings.LocalAddress();
            context.Container.ConfigureComponent<EventSourcedUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork)
                           .ConfigureProperty(p => p.EndpointAddress, localAddress);
            context.Container.ConfigureComponent<TransactionalUnitOfWork>(DependencyLifecycle.InstancePerCall)
                               .ConfigureProperty(p => p.EndpointAddress, localAddress);

            context.Container.ConfigureComponent<MessageSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.EndpointAddress, localAddress);
            context.Container.ConfigureComponent<MessagePublisher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.EndpointAddress, localAddress);

            context.Container.ConfigureComponent<EventSourcedRouterProjectionCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<TransactionalRouterProjectionCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<SubscriptionsProjectionCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<ReceiverSinkProjectionCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<EventStoreQueueCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<DequeueStrategy>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<SubscriptionManager>(DependencyLifecycle.SingleInstance)
                       .ConfigureProperty(p => p.EndpointAddress, localAddress);

        }

        /// <summary>
        /// Returns false. Uses shared connection configured via <see cref="EventStoreConnectionManager"/> feature.
        /// </summary>
        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "singleNode=127.0.0.1"; }
        }
    }
}