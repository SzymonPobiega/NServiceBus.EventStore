using NServiceBus.Config;
using NServiceBus.Settings;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Config;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Features
{
    internal class EventStoreTransportFeature : ConfigureTransport
    {
        protected override void Configure(FeatureConfigurationContext context, string connectionString)
        {            
            if (!context.Container.HasComponent<IConnectionConfiguration>())
            {
                var connectionConfiguration = new ConnectionStringParser().Parse(connectionString);

                context.Container.RegisterSingleton<IConnectionConfiguration>(connectionConfiguration);
                context.Container.ConfigureComponent<DefaultConnectionManager>(DependencyLifecycle.SingleInstance);

            }

            var localAddress = context.Settings.LocalAddress();
            context.Container.ConfigureComponent<EventSourcedUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork)
                           .ConfigureProperty(p => p.EndpointAddress, localAddress);
            context.Container.ConfigureComponent<TransactionalUnitOfWork>(DependencyLifecycle.InstancePerCall)
                               .ConfigureProperty(p => p.EndpointAddress, localAddress);

            context.Container.ConfigureComponent<MessageSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.EndpointAddress, localAddress);
            context.Container.ConfigureComponent<MessagePublisher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.EndpointAddress, localAddress);

            context.Container.ConfigureComponent<RouterProjectionCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<SubscriptionsProjectionCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<ReceiverSinkProjectionCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<CompositeQueueCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<DequeueStrategy>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<SubscriptionManager>(DependencyLifecycle.SingleInstance)
                       .ConfigureProperty(p => p.EndpointAddress, localAddress);

            context.Container.ConfigureComponent<DefaultConnectionManager>(DependencyLifecycle.SingleInstance);
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "singleNode=127.0.0.1"; }
        }
    }
}