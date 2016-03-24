using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;

namespace PerformanceTest
{
    class Program
    {
        public const int MessageCount = 1000;
        const int SendThreadCount = 10;
        public static int Received;

        static void Main(string[] args)
        {
            var endpointConfig = new EndpointConfiguration();
            endpointConfig.UseTransport<EventStoreTransport>().ConnectionString("singleNode=127.0.0.1;user=admin;password=changeit");
            endpointConfig.UsePersistence<EventStorePersistence>();
            endpointConfig.SendFailedMessagesTo("error");

            Run(endpointConfig).GetAwaiter().GetResult();
        }

        static async Task Run(EndpointConfiguration endpointConfig)
        {
            var startable = await Endpoint.Create(endpointConfig).ConfigureAwait(false);
            var started = await startable.Start().ConfigureAwait(false);

            Console.WriteLine("Press <enter> to start sending messages.");
            Console.ReadLine();

            var threads = Enumerable.Range(0, SendThreadCount).Select(i => new Thread(() =>
            {
                var toSend = MessageCount/SendThreadCount;
                for (var j = 0; j < toSend; j++)
                {
                    //started.SendLocal(new MyMessage()).GetAwaiter().GetResult();
                    var options = new SendOptions();
                    options.SetDestination("null");
                    started.Send(new MyMessage(), options).GetAwaiter().GetResult();
                }
            })).ToArray();

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine($"Sent all {MessageCount} messages.");

            Console.ReadLine();
            await started.Stop();
        }
    }

    class MyMessageHandler : IHandleMessages< MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            if (Interlocked.Increment(ref Program.Received) == Program.MessageCount)
            {
                Console.WriteLine("Received all messages.");
            }
            return Task.FromResult(0);
        }
    }

    class MyMessage : IMessage
    {
    }
}
