using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Exchange
{
    class ExchangeCollection
    {
        public ExchangeCollection(ExchangeDataCollection dataCollection)
        {
            var toProcess = new List<ExchangeData>(dataCollection.Exchanges);
            var workingCollection = new List<Exchange>();

            while (toProcess.Any())
            {
                BuildExchange(workingCollection, toProcess, dataCollection.Exchanges);
            }

            exchanges = workingCollection.ToArray();
        }

        static Exchange BuildExchange(ICollection<Exchange> exchanges, ICollection<ExchangeData> toProcess, IReadOnlyCollection<ExchangeData> allExchangeData)
        {
            var next = toProcess.First();
            toProcess.Remove(next);
            var downstreamExchangeNames = allExchangeData
                .Where(e => e.IncomingBindings.Any(i => i.Equals(next.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(e => e.Name);

            var downstreamExchanges = downstreamExchangeNames.Select(n =>
            {
                var de = exchanges.FirstOrDefault(e => e.Name.Equals(n, StringComparison.OrdinalIgnoreCase));
                return de ?? BuildExchange(exchanges, toProcess, allExchangeData);
            });
            var newExchange = new Exchange(next.Name, downstreamExchanges.ToArray(), next.OutgoingQueueBindings.ToArray());
            exchanges.Add(newExchange);
            return newExchange;
        }

        public IEnumerable<string> GetDestinationQueues(string destinationExchange)
        {
            var exchange = exchanges.FirstOrDefault(e => e.Name == destinationExchange);
            if (exchange == null)
            {
                return Enumerable.Empty<string>();
            }
            return exchange.GetDestinationQueues();
        }

        Exchange[] exchanges;
    }
}