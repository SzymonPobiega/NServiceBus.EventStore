using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace NServiceBus
{
    static class EventStoreConnectionExtensions
    {
        public static async Task EnsureClosed(this IEventStoreConnection connection, TimeSpan? timeout = null)
        {
            var closeEvent = new TaskCompletionSource<bool>();
            connection.Closed += (sender, args) =>
            {
                closeEvent.SetResult(true);
            };
            connection.Close();

            if (await Task.WhenAny(closeEvent.Task, Task.Delay(1000)) != closeEvent.Task)
            {
                Console.WriteLine("Failed to close connection {0}", connection.ConnectionName);
            }
        }
    }
}