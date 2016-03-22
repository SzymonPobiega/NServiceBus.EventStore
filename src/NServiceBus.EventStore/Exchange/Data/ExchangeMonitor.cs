using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Logging;

namespace NServiceBus.Exchange
{
    class ExchangeMonitor
    {
        public ExchangeMonitor(IEventStoreConnection connection, Action<ExchangeDataCollection> newVersionDetectedCallback, CriticalError criticalError)
        {
            this.connection = connection;
            this.newVersionDetectedCallback = newVersionDetectedCallback;
            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("EventStoreExchangeMonitor", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to re-subscribe to changes in exchanges.", ex));
        }

        public void StartMonitoring()
        {
            subscription = connection.SubscribeToStreamAsync(ExchangeRepository.StreamName, true, OnNewVersion, OnSubscriptionDropped).GetAwaiter().GetResult();
        }

        public void StopMonitoring()
        {
            subscription.Unsubscribe();
        }

        void OnSubscriptionDropped(EventStoreSubscription sub, SubscriptionDropReason reason, Exception error)
        {
            if (reason == SubscriptionDropReason.UserInitiated)
            {
                return;
            }
            Logger.Error("Exchanges subscription dropped.", error);
            try
            {
                subscription = connection.SubscribeToStreamAsync(ExchangeRepository.StreamName, true, OnNewVersion, OnSubscriptionDropped)
                    .GetAwaiter()
                    .GetResult();
                circuitBreaker.Success();
            }
            catch (Exception ex)
            {
                circuitBreaker.Failure(ex).GetAwaiter().GetResult();
            }
        }

        void OnNewVersion(EventStoreSubscription sub, ResolvedEvent @event)
        {
            //var exchangesData = @event.Event.Data.ParseJson<ExchangeDataCollection>();
            //newVersionDetectedCallback(exchangesData);
        }

        static ILog Logger = LogManager.GetLogger<ExchangeRepository>();

        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;
        IEventStoreConnection connection;
        EventStoreSubscription subscription;
        Action<ExchangeDataCollection> newVersionDetectedCallback;
    }
}