using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Exchange;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Transports;

namespace NServiceBus
{
    class SubscriptionManager : IManageSubscriptions
    {
        bool started;
        ExchangeManager exchangeManager;
        string localQueue;
        IEventStoreConnection managerConnection;
        readonly ConcurrentDictionary<Type, string> typeTopologyConfiguredSet = new ConcurrentDictionary<Type, string>();

        public SubscriptionManager(IConnectionConfiguration connectionConfig, string localQueue, bool enableCaching)
        {
            managerConnection = connectionConfig.CreateConnection();
            exchangeManager = new ExchangeManager(managerConnection, enableCaching);
            this.localQueue = localQueue;
            Start().GetAwaiter().GetResult();
        }

        async Task Start()
        {
            if (started)
            {
                return;
            }
            await managerConnection.ConnectAsync().ConfigureAwait(false);
            await exchangeManager.Start().ConfigureAwait(false);
            started = true;
        }

        public void Stop()
        {
            exchangeManager.Stop();
            managerConnection.Dispose();
            started = false;
        }

        public async Task<IEnumerable<string>> GetDestinationQueues(Type messageType)
        {
            if (!IsTypeTopologyKnownConfigured(messageType))
            {
                await exchangeManager.UpdateExchanges(c =>
                {
                    SetupTypeSubscriptions(messageType, c);
                }).ConfigureAwait(false);
            }
            return await exchangeManager.GetDestinationQueues(ExchangeName(messageType)).ConfigureAwait(false);
        }

        public async Task Subscribe(Type eventType, ContextBag context)
        {
            if (eventType == typeof(IEvent))
            {
                // Make handlers for IEvent handle all events whether they extend IEvent or not
                eventType = typeof(object);
            }
            await exchangeManager.UpdateExchanges(c =>
            {
                SetupTypeSubscriptions(eventType, c);
                c.BindQueue(ExchangeName(eventType), localQueue);

            }).ConfigureAwait(false);
            MarkTypeConfigured(eventType);
        }

        public Task Unsubscribe(Type eventType, ContextBag context)
        {
            return exchangeManager.UnbindQueue(ExchangeName(eventType), localQueue);
        }

        static string ExchangeName(Type type)
        {
            return type.Namespace + "-" + type.Name;
        }

        void SetupTypeSubscriptions(Type type, ExchangeDataCollection exchangeDataCollection)
        {
            if (type == typeof(object) || IsTypeTopologyKnownConfigured(type))
            {
                return;
            }

            var typeToProcess = type;
            exchangeDataCollection.DeclareExchange(ExchangeName(typeToProcess));

            exchangeDataCollection.DeclareExchange(ExchangeName(typeToProcess));
            var baseType = typeToProcess.BaseType;
            while (baseType != null)
            {
                exchangeDataCollection.DeclareExchange(ExchangeName(baseType));
                exchangeDataCollection.BindExchange(ExchangeName(typeToProcess), ExchangeName(baseType));
                typeToProcess = baseType;
                baseType = typeToProcess.BaseType;
            }

            foreach (var exchangeName in type.GetInterfaces().Select(ExchangeName))
            {
                exchangeDataCollection.DeclareExchange(exchangeName);
                exchangeDataCollection.BindExchange(ExchangeName(type), exchangeName);
            }
        }

        void MarkTypeConfigured(Type eventType)
        {
            typeTopologyConfiguredSet[eventType] = null;
        }

        bool IsTypeTopologyKnownConfigured(Type eventType)
        {
            return typeTopologyConfiguredSet.ContainsKey(eventType);
        }
    }
}