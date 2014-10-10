using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Features;

namespace NServiceBus.Persistence.EventStore.TimeoutPersister
{
    class EventStoreTimeoutReaderTask : FeatureStartupTask
    {
        public EventStoreTimeoutPersister Persister { get; set; }

        protected override void OnStart()
        {
            tokenSource = new CancellationTokenSource();
            readTask = Task.Factory.StartNew(() => Persister.Read(tokenSource.Token));
        }

        protected override void OnStop()
        {
            base.OnStop();
            tokenSource.Cancel();
            readTask.Wait();
        }

        private CancellationTokenSource tokenSource;
        private Task readTask;
    }
}