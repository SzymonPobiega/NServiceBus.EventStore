using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Exchange
{
    class Exchange
    {
        private Exchange[] downstreamExchanges;
        private string[] downstreamQueues;
        public string Name { get; }

        public Exchange(string name, Exchange[] downstreamExchanges, string[] downstreamQueues)
        {
            this.downstreamExchanges = downstreamExchanges;
            this.downstreamQueues = downstreamQueues;
            Name = name;
        }

        public IEnumerable<string> GetDestinationQueues()
        {
            foreach (var queue in downstreamQueues)
            {
                yield return queue;
            }
            foreach (var queue in downstreamExchanges.SelectMany(e => e.GetDestinationQueues()))
            {
                yield return queue;
            }
        }
    }
}