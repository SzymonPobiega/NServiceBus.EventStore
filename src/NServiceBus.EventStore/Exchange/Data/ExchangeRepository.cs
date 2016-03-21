using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using Newtonsoft.Json;
using NServiceBus.Internal;

namespace NServiceBus.Exchange
{
    class ExchangeRepository
    {
        const string StreamName = "nsb-exchanges";
        IEventStoreConnection connection;
        EventStoreSubscription subscription;
        Action<ExchangeDataCollection> newVersionDetectedCallback;

        public ExchangeRepository(IEventStoreConnection connection)
        {
            this.connection = connection;
        }

        public async Task StartMonitoring(Action<ExchangeDataCollection> newVersionDetectedCallback)
        {
            this.newVersionDetectedCallback = newVersionDetectedCallback;
            subscription = await connection.SubscribeToStreamAsync(StreamName, true, OnNewVersion, OnSubscriptionDropped).ConfigureAwait(false);
        }

        public void StopMonitoring()
        {
            subscription.Unsubscribe();
        }

        void OnSubscriptionDropped(EventStoreSubscription sub, SubscriptionDropReason reason, Exception error)
        {
            if (reason != SubscriptionDropReason.UserInitiated)
            {
                subscription = connection.SubscribeToStreamAsync(StreamName, true, OnNewVersion, OnSubscriptionDropped)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        void OnNewVersion(EventStoreSubscription sub, ResolvedEvent @event)
        {
            var exchangesData = @event.Event.Data.ParseJson<ExchangeDataCollection>();
            newVersionDetectedCallback(exchangesData);
        }

        public Task DeclareExchange(string name)
        {
            return UpdateExchanges(c => c.DeclareExchange(name));
        }

        public Task BindExchange(string upstream, string downstream)
        {
            return UpdateExchanges(c => c.BindExchange(upstream, downstream));
        }

        public Task BindQueue(string exchange, string queue)
        {
            return UpdateExchanges(c => c.BindQueue(exchange, queue));
        }

        public Task UnbindQueue(string exchange, string queue)
        {
            return UpdateExchanges(c => c.UnbindQueue(exchange, queue));
        }

        public async Task<ExchangeDataCollection> LoadExchanges()
        {
            var readResult = await GetExchanges().ConfigureAwait(false);
            return readResult.Item1;
        }

        public async Task UpdateExchanges(Action<ExchangeDataCollection> updateAction)
        {
            var succeeded = false;
            while (!succeeded)
            {
                var readResult = await GetExchanges().ConfigureAwait(false);
                var exchangeCollection = readResult.Item1;
                updateAction(exchangeCollection);
                string instring = JsonConvert.SerializeObject(exchangeCollection, Formatting.Indented, Json.JsonSettings);
                var data = Json.UTF8NoBom.GetBytes(instring);
                var eventData = new EventData(Guid.NewGuid(), "exchange-data", true, data, new byte[0]);
                try
                {
                    await connection.AppendToStreamAsync(StreamName, readResult.Item2, eventData).ConfigureAwait(false);
                    succeeded = true;
                }
                catch (WrongExpectedVersionException)
                {
                    //Ignore
                }
            }
        }


        async Task<Tuple<ExchangeDataCollection, int>> GetExchanges()
        {
            int expectedVersion;
            ExchangeDataCollection exchangesData;
            var readResult = await connection.ReadStreamEventsBackwardAsync(StreamName, -1, 1, true).ConfigureAwait(false);
            if (readResult.Status == SliceReadStatus.Success)
            {
                var @event = readResult.Events[0].Event;
                exchangesData = @event.Data.ParseJson<ExchangeDataCollection>();
                expectedVersion = @event.EventNumber;
            }
            else
            {
                exchangesData = new ExchangeDataCollection();
                expectedVersion = ExpectedVersion.NoStream;
            }
            return Tuple.Create(exchangesData, expectedVersion);
        }
    }
}