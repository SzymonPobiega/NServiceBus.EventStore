using System;
using System.Threading;
using EventStore.ClientAPI.Common.Utils;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Integration
{
    public abstract class SendAndReceiveTest : IntegrationTest
    {
        private ManualResetEventSlim Event;
        protected int Count;
        private int targetCount;
        private DequeueStrategy dequeueStrategy;

        [SetUp]
        public void SendAndReceiveSetUp()
        {
            dequeueStrategy = new DequeueStrategy(new DefaultConnectionManager(ConnectionConfiguration));
            Event = new ManualResetEventSlim();
            dequeueStrategy.Init(ReceiverAddress, TransactionSettings.Default,
                                 x =>
                                     {
                                         Assert.AreEqual(SenderAddress, x.ReplyToAddress);
                                         Assert.AreEqual("correlation", x.CorrelationId);
                                         x.Body.ParseJson<int>();
                                         if (Interlocked.Increment(ref Count) == targetCount)
                                         {
                                             Event.Set();
                                         }
                                         return true;
                                     },
                                 (m, e) =>
                                     {

                                     });
            dequeueStrategy.Start(1);
        }

        protected bool ExpectReceive(int messageNumber, TimeSpan timeout)
        {
            Event.Reset();
            Count = 0;
            targetCount = messageNumber;
            return Event.Wait(timeout);
        }

        protected void SendMessages(ISendMessages sender, int count)
        {
            for (var i = 0; i < count; i++)
            {
                sender.Send(GenerateTransportMessage(i, "MessageType"), ReceiverAddress);
            }
        }

        protected void PublishMessages(IPublishMessages publisher, int count, Type eventType)
        {
            for (var i = 0; i < count; i++)
            {
                publisher.Publish(GenerateTransportMessage(i, eventType.FullName), new[]{eventType});
            }
        }

        private TransportMessage GenerateTransportMessage(int i, string messagetype)
        {
            var message = new TransportMessage()
                {
                    ReplyToAddress = SenderAddress,
                    CorrelationId = "correlation",
                    Body = i.ToJsonBytes()
                };
            message.Headers[Headers.EnclosedMessageTypes] = messagetype;
            message.Headers[Headers.ContentType] = ContentTypes.Json;
            return message;
        }  

    }
}