using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Exchange
{
    class ExchangeDataCollection
    {
        public List<ExchangeData> Exchanges { get; } = new List<ExchangeData>();

        public void DeclareExchange(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (Exchanges.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            Exchanges.Add(new ExchangeData()
            {
                Name = name
            });
        }

        public void BindExchange(string upstreamExchangeName, string downstreamExchangeName)
        {
            if (upstreamExchangeName == null)
            {
                throw new ArgumentNullException(nameof(upstreamExchangeName));
            }
            if (downstreamExchangeName == null)
            {
                throw new ArgumentNullException(nameof(downstreamExchangeName));
            }
            var downstreamExchange = Exchanges.Single(e => e.Name.Equals(downstreamExchangeName, StringComparison.OrdinalIgnoreCase));
            if (downstreamExchange.IncomingBindings.Any(e => e.Equals(upstreamExchangeName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            downstreamExchange.IncomingBindings.Add(upstreamExchangeName);
        }

        public void BindQueue(string exchangeName, string queueName)
        {
            if (exchangeName == null)
            {
                throw new ArgumentNullException(nameof(exchangeName));
            }
            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }
            var exchange = Exchanges.Single(e => e.Name.Equals(exchangeName, StringComparison.OrdinalIgnoreCase));
            if (exchange.OutgoingQueueBindings.Any(q => q.Equals(queueName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            exchange.OutgoingQueueBindings.Add(queueName);
        }

        public void UnbindQueue(string exchangeName, string queueName)
        {
            if (exchangeName == null)
            {
                throw new ArgumentNullException(nameof(exchangeName));
            }
            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }
            var exchange = Exchanges.FirstOrDefault(e => e.Name.Equals(exchangeName, StringComparison.OrdinalIgnoreCase));
            exchange?.OutgoingQueueBindings.RemoveAll(q => q.Equals(queueName, StringComparison.OrdinalIgnoreCase));
        }
    }
}