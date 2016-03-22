using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using Newtonsoft.Json;
using NServiceBus.Internal;
using NServiceBus.Logging;

namespace NServiceBus.Exchange
{
    class ExchangeRepository
    {
        public const string StreamName = "nsb-exchanges";
        IEventStoreConnection connection;

        public ExchangeRepository(IEventStoreConnection connection)
        {
            this.connection = connection;
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