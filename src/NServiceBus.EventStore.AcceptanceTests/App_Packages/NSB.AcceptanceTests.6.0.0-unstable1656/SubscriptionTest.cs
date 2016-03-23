using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NUnit.Framework;

namespace NServiceBus.EventStore.AcceptanceTests.App_Packages.NSB.AcceptanceTests._6._0
{
    [TestFixture]
    public class SubscriptionTest
    {
        [Test]
        public void SyncTestWrapper()
        {
            VerifyThatSubscriptionWork().GetAwaiter().GetResult();
        }

        async Task VerifyThatSubscriptionWork()
        {
            var closeEvent = new ManualResetEventSlim(false);
            var settings = ConnectionSettings.Create().UseConsoleLogger().EnableVerboseLogging();
            var connection = EventStoreConnection.Create(settings, new IPEndPoint(IPAddress.Loopback, 1113));

            await connection.ConnectAsync().ConfigureAwait(false);
            var streamName = Guid.NewGuid().ToString();

            var sub = await connection.SubscribeToStreamAsync(streamName, true, (subscription, e) => { }).ConfigureAwait(false);
            //var sub = connection.SubscribeToStreamAsync(streamName, true, (subscription, e) => { }).GetAwaiter().GetResult();

            //sub.Unsubscribe();

            //Verify closed
            connection.Closed += (sender, args) =>
            {
                Console.WriteLine("{0}:>> Closed: "+args.Reason, DateTime.UtcNow.ToLongTimeString());
                closeEvent.Set();
            };
            Console.WriteLine("{0}>> Calling Close", DateTime.UtcNow.ToLongTimeString());
            connection.Close();
            if (!closeEvent.Wait(TimeSpan.FromSeconds(10)))
            {
                Console.WriteLine("{1}>> Failed to close connection {0}", connection.ConnectionName, DateTime.UtcNow.ToLongTimeString());
            }
            else
            {
                Console.WriteLine("{0}>> Close signalled via event", DateTime.UtcNow.ToLongTimeString());
            }
        }
    }
}