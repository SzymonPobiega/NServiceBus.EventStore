using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;

namespace PerformanceTest
{
    class Program
    {
        public const int MessageCount = 50000;
        const int SendThreadCount = 5;
        public static ManualResetEventSlim ReceivingDoneEvent = new ManualResetEventSlim(false);
        public static Stopwatch ReceiveWatch = new Stopwatch();

        static void Main(string[] args)
        {
            var receiverConfig = BuildReceiverConfig();
            InstallReceiver(receiverConfig).GetAwaiter().GetResult();

            var senderConfig = new EndpointConfiguration("Sender");
            var routing = senderConfig.UseTransport<EventStoreTransport>().ConnectionString("singleNode=127.0.0.1;user=admin;password=changeit").Routing();
            routing.RouteToEndpoint(typeof(MyMessage), "Receiver");
            senderConfig.UsePersistence<EventStorePersistence>();
            senderConfig.SendFailedMessagesTo("error");
            senderConfig.EnableInstallers(null);

            RunSender(senderConfig).GetAwaiter().GetResult();

            receiverConfig = BuildReceiverConfig();

            RunReceiver(receiverConfig).GetAwaiter().GetResult();
        }

        static EndpointConfiguration BuildReceiverConfig()
        {
            var receiverConfig = new EndpointConfiguration("Receiver");
            receiverConfig.UseTransport<EventStoreTransport>()
                .ConnectionString("singleNode=127.0.0.1;user=admin;password=changeit");
            receiverConfig.UsePersistence<EventStorePersistence>();
            receiverConfig.SendFailedMessagesTo("error");
            return receiverConfig;
        }

        static async Task RunReceiver(EndpointConfiguration endpointConfig)
        {
            Console.WriteLine("Receiving");
            var startable = await Endpoint.Create(endpointConfig).ConfigureAwait(false);
            var started = await startable.Start().ConfigureAwait(false);

            ReceivingDoneEvent.Wait();
            ReceiveWatch.Stop();
            Console.WriteLine($"Sent all {MessageCount} messages in {ReceiveWatch.ElapsedMilliseconds} ms.");
            Console.ReadLine();
            await started.Stop();
        }

        static async Task InstallReceiver(EndpointConfiguration endpointConfig)
        {
            endpointConfig.EnableInstallers(null);
            Console.WriteLine("Installing receiver");
            var startable = await Endpoint.Create(endpointConfig).ConfigureAwait(false);
            var started = await startable.Start().ConfigureAwait(false);
            await started.Stop();
        }

        static async Task RunSender(EndpointConfiguration endpointConfig)
        {
            var startable = await Endpoint.Create(endpointConfig).ConfigureAwait(false);
            var started = await startable.Start().ConfigureAwait(false);
            var doneEvent = new ManualResetEventSlim(false);

            Console.WriteLine("Press <enter> to start sending messages.");
            Console.ReadLine();
            var threads = new List<Thread>();
            for (var i = 0; i < SendThreadCount; i++)
            {
                threads.Add(CreateSenderThread(started, doneEvent));
            }
            var sw = Stopwatch.StartNew();
            sw.Start();
            threads.ForEach(thread => thread.Start());
            doneEvent.Wait();
            sw.Stop();

            Console.WriteLine($"Sent all {MessageCount} messages in {sw.ElapsedMilliseconds} ms.");

            Console.ReadLine();
            await started.Stop();
        }

        static Thread CreateSenderThread(IMessageSession messageSession, ManualResetEventSlim doneEvent)
        {
            return new Thread(() =>
            {
                var succ = 0;
                var messagesPerThread = MessageCount / SendThreadCount;
                for (var j = 0; j < messagesPerThread; ++j)
                {
                    var task = messageSession.Send(new MyMessage());
                    task.ContinueWith(x =>
                    {
                        if (x.IsFaulted)
                        {
                            Console.WriteLine("Error!" + x.Exception);
                            return;
                        }
                        var localAll = Interlocked.Increment(ref succ);
                        if (localAll == messagesPerThread)
                        {
                            doneEvent.Set();
                        }
                    });
                }
            })
            {IsBackground = true};
        }
    }

    class MyMessageHandler : IHandleMessages< MyMessage>
    {
        static int received;

        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            var localReceived = Interlocked.Increment(ref received);
            if (localReceived == Program.MessageCount)
            {
                Program.ReceivingDoneEvent.Set();
            }
            else if (localReceived == 1)
            {
                Program.ReceiveWatch.Start();
            }
            return Task.FromResult(0);
        }
    }

    class MyMessage : IMessage
    {
    }
}
