using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.DelayedDelivery;
using NServiceBus.Internal;
using NServiceBus.Performance.TimeToBeReceived;
using NServiceBus.Routing;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus
{
    class TransportInfrastructure : Transport.TransportInfrastructure
    {
        bool started;
        bool stopped;
        SettingsHolder settings;
        ConnectionConfiguration connectionConfiguration;
        Lazy<SubscriptionManager> subscriptionManager;
        Lazy<TimeoutProcessor> timeoutProcessor; 

        public TransportInfrastructure(SettingsHolder settings, string connectionString)
        {
            connectionConfiguration = new ConnectionStringParser().Parse(connectionString);
            subscriptionManager = new Lazy<SubscriptionManager>(() =>
            {
                var disableCaching = settings.GetOrDefault<bool>("NServiceBus.EventStore.DisableExchangeCaching");
                return new SubscriptionManager(connectionConfiguration, settings.LocalAddress(), !disableCaching);
            });
            timeoutProcessor = new Lazy<TimeoutProcessor>(() =>
            {
                var uniqueId = settings.GetOrDefault<string>("NServiceBus.EventStore.TimeoutProcessorId") ?? Guid.NewGuid().ToString();
                return new TimeoutProcessor(() => DateTime.UtcNow, uniqueId, connectionConfiguration);
            });
            this.settings = settings;
        }

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            return new TransportReceiveInfrastructure(
                () => new MessagePump(connectionConfiguration, Start, Stop),
                () => new QueueCreator(connectionConfiguration), PreStartupCheck 
                );
        }

        async Task Start(CriticalError criticalError)
        {
            if (started)
            {
                return;
            }
            await subscriptionManager.Value.Start(criticalError).ConfigureAwait(false);
            await timeoutProcessor.Value.Start().ConfigureAwait(false);
            started = true;
        }

        new async Task Stop()
        {
            if (stopped)
            {
                return;
            }
            await timeoutProcessor.Value.Stop().ConfigureAwait(false);
            await subscriptionManager.Value.Stop();
            stopped = true;
        }

        private static Task<StartupCheckResult> PreStartupCheck()
        {
            return Task.FromResult(StartupCheckResult.Success);
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {            
            return new TransportSendInfrastructure(
                () => new Dispatcher(connectionConfiguration, subscriptionManager.Value, timeoutProcessor.Value), PreStartupCheck);
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            return new TransportSubscriptionInfrastructure(() => subscriptionManager.Value);
        }

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
        {
            return instance;
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            var queue = new StringBuilder(logicalAddress.EndpointInstance.Endpoint.ToString());
            if (logicalAddress.EndpointInstance.Discriminator != null)
            {
                queue.Append("-" + logicalAddress.EndpointInstance.Discriminator);
            }
            if (logicalAddress.Qualifier != null)
            {
                queue.Append("_" + logicalAddress.Qualifier);
            }
            return queue.ToString();
        }
        public override IEnumerable<Type> DeliveryConstraints { get; } = new[]
        {
            typeof(DiscardIfNotReceivedBefore),
            typeof(DelayDeliveryWith),
            typeof(DoNotDeliverBefore)
        };

        public override TransportTransactionMode TransactionMode => TransportTransactionMode.ReceiveOnly;

        public override OutboundRoutingPolicy OutboundRoutingPolicy => new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Multicast, OutboundRoutingType.Unicast);
    }
}