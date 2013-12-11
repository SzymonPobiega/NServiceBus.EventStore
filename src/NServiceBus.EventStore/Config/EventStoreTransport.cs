using NServiceBus.Config;
using NServiceBus.Settings;
using NServiceBus.Transports.EventStore.Serializers.Json.Config;

namespace NServiceBus.Transports.EventStore.Config
{
    public class EventStoreTransport : ConfigureTransport<EventStore>
    {
        public override void Initialize()
        {
            var connectionString = SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");
            var connectionConfiguration = new ConnectionStringParser().Parse(connectionString);

            NServiceBus.Configure.Instance.Configurer.RegisterSingleton<IConnectionConfiguration>(connectionConfiguration);
            NServiceBus.Configure.Component<DefaultConnectionManager>(DependencyLifecycle.SingleInstance);

            NServiceBus.Configure.Component<EventSourcedUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork)
                           .ConfigureProperty(p => p.EndpointAddress, Address.Local);
            NServiceBus.Configure.Component<TransactionalUnitOfWork>(DependencyLifecycle.InstancePerCall)
                               .ConfigureProperty(p => p.EndpointAddress, Address.Local);

            NServiceBus.Configure.Component<MessageSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.EndpointAddress, Address.Local);            
            NServiceBus.Configure.Component<MessagePublisher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.EndpointAddress, Address.Local);

            NServiceBus.Configure.Component<TransactionalModeRouterProjectionCreator>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<EventSourcedModeRouterProjectionCreator>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<ReceiverSinkProjectionCreator>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<CompositeQueueCreator>(DependencyLifecycle.InstancePerCall);

            NServiceBus.Configure.Component<DequeueStrategy>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<SubscriptionManager>(DependencyLifecycle.SingleInstance)
                       .ConfigureProperty(p => p.EndpointAddress, Address.Local);
            
            InfrastructureServices.Enable<IManageEventStoreConnections>();

            Features.Categories.Serializers.SetDefault<JsonNoBomSerialization>();
        }


        protected override void InternalConfigure(Configure config)
        {
            Enable<EventStoreTransport>();
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "singleNode=127.0.0.1"; }
        }
    }
}