﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;
using NServiceBus.Internal;
using NServiceBus.Performance.TimeToBeReceived;
using NServiceBus.Routing;
using NServiceBus.Settings;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;

namespace NServiceBus
{
    public class EventStoreTransport : TransportDefinition
    {
        protected override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new EventStoreTransportInfrastructure(settings, connectionString);
        }

        public override string ExampleConnectionStringForErrorMessage => "singleNode=127.0.0.1";
    }

    class EventStoreTransportInfrastructure : TransportInfrastructure
    {
        SettingsHolder settings;
        ConnectionConfiguration connectionConfiguration;

        public EventStoreTransportInfrastructure(SettingsHolder settings, string connectionString)
        {
            connectionConfiguration = new ConnectionStringParser().Parse(connectionString);
            this.settings = settings;
        }

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            return new TransportReceiveInfrastructure(
                () => new MessagePump(connectionConfiguration),
                () => new EventStoreQueueCreator(new List<IRegisterProjections>()
                {
                    new ReceiverSinkProjectionCreator(),
                    new RouterProjectionCreator(),
                    new SubscriptionsProjectionCreator()
                }, connectionConfiguration), PreStartupCheck 
                );
        }

        private static Task<StartupCheckResult> PreStartupCheck()
        {
            return Task.FromResult(StartupCheckResult.Success);
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            return new TransportSendInfrastructure(
                () => new Dispatcher(connectionConfiguration, settings.EndpointName().ToString()), PreStartupCheck);
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            return new TransportSubscriptionInfrastructure(() => new SubscriptionManager(connectionConfiguration, settings.EndpointName().ToString()));
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
            typeof(DiscardIfNotReceivedBefore)
        };

        public override TransportTransactionMode TransactionMode => TransportTransactionMode.ReceiveOnly;

        public override OutboundRoutingPolicy OutboundRoutingPolicy => new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Multicast, OutboundRoutingType.Unicast);
    }
}