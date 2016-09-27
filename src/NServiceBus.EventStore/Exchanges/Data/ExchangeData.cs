using System.Collections.Generic;

namespace NServiceBus
{
    class ExchangeData
    {
        public string Name { get; set; }
        public List<string> IncomingBindings { get; } = new List<string>();
        public List<string> OutgoingQueueBindings { get; } = new List<string>();
    }
}
