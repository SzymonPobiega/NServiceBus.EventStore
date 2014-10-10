using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Features;

namespace NServiceBus.Persistence.EventStore.TimeoutPersister
{
    class EventStoreTimeoutHeartbeatTask : FeatureStartupTask
    {
        public EventStoreTimeoutPersister Persister { get; set; }

        protected override void OnStart()
        {
            tokenSource = new CancellationTokenSource();
            heartbeatTask = Task.Factory.StartNew(() => Persister.Heartbeat(tokenSource.Token));
        }

        protected override void OnStop()
        {
            base.OnStop();
            tokenSource.Cancel();
            heartbeatTask.Wait();
        }

        private CancellationTokenSource tokenSource;
        private Task heartbeatTask;
    }
}