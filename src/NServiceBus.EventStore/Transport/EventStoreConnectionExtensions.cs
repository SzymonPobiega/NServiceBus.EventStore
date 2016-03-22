using System;
using System.Threading;
using EventStore.ClientAPI;

namespace NServiceBus
{
    public static class EventStoreConnectionExtensions
    {
        public static void EnsureClosed(this IEventStoreConnection connection, TimeSpan? timeout = null)
        {
            var closeEvent = new ManualResetEventSlim(false);
            connection.Closed += (sender, args) =>
            {
                closeEvent.Set();
            };
            connection.Close();
            if (!closeEvent.Wait(timeout ?? TimeSpan.FromSeconds(1)))
            {
                Console.WriteLine("Failed to close connection  {0}", connection.ConnectionName);
            }
        }
    }
}