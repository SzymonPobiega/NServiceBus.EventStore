using NServiceBus.Config;
using NServiceBus.Settings;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Transports.EventStore.Config;

namespace NServiceBus.Features
{
    public class EventStoreTransport : ConfigureTransport<EventStore>
    {
        public override void Initialize()
        {
            if (!SettingsHolder.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
            {
                Address.InitializeLocalAddress(Address.Local.Queue + "." + Address.Local.Machine);
            }
            if (!NServiceBus.Configure.HasComponent<IConnectionConfiguration>())
            {
                NServiceBus.Configure.Instance.EventStore();
            }

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