using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Logging;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Transports.EventStore
{
    public class MessagePump : IPushMessages
    {
        private readonly IManageEventStoreConnections connectionManager;

        public MessagePump(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        private void SubscriptionDropped(EventStorePersistentSubscription droppedSubscription, SubscriptionDropReason dropReason, Exception e)
        {
            Logger.Error("Subscription dropped", e);
            try
            {
                subscription = connectionManager.GetConnection().ConnectToPersistentSubscription(inputQueue, inputQueue, OnEvent, SubscriptionDropped);
            }
            catch (Exception ex)
            {
                criticalError.Raise("Can't reconnect to EventStore", ex);
            }
        }

        private void OnEvent(EventStorePersistentSubscription subscription, ResolvedEvent evnt)
        {
            concurrencyLimiter.Wait(cancellationToken);

            var tokenSource = new CancellationTokenSource();
            var pushContext = ToPushContext(evnt);
            if (pushContext == null) //system message
            {
                return;
            }
            var receiveTask = Task.Run(() =>
            {

                try
                {
                    pipeline(pushContext).GetAwaiter().GetResult();
                    receiveCircuitBreaker.Success();
                }
                finally
                {
                    concurrencyLimiter.Release();
                }
            }, tokenSource.Token).ContinueWith(t => tokenSource.Dispose());

            runningReceiveTasks.TryAdd(receiveTask, receiveTask);

            // We insert the original task into the runningReceiveTasks because we want to await the completion
            // of the running receives. ExecuteSynchronously is a request to execute the continuation as part of
            // the transition of the antecedents completion phase. This means in most of the cases the continuation
            // will be executed during this transition and the antecedent task goes into the completion state only 
            // after the continuation is executed. This is not always the case. When the TPL thread handling the
            // antecedent task is aborted the continuation will be scheduled. But in this case we don't need to await
            // the continuation to complete because only really care about the receive operations. The final operation
            // when shutting down is a clear of the running tasks anyway.
            receiveTask.ContinueWith(t =>
            {
                Task toBeRemoved;
                runningReceiveTasks.TryRemove(t, out toBeRemoved);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        static PushContext ToPushContext(ResolvedEvent evnt)
        {
            if (evnt.Event.EventType.StartsWith("$"))
            {
                return null;
            }
            var metadata = evnt.Event.Metadata.ParseJson<EventStoreMessageMetadata>();
            var headers = metadata.Headers.ToDictionary(x => x.Key.ToPascalCase(), x => x.Value);
            var context = new PushContext(metadata.MessageId, headers, new MemoryStream(evnt.Event.Data), new TransportTransaction(), null, new ContextBag());
            return context;
        }

        public Task Init(Func<PushContext, Task> pipe, CriticalError criticalError, PushSettings settings)
        {
            pipeline = pipe;
            inputQueue = settings.InputQueue;
            receiveCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("EventStoreReceive", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to receive from " + settings.InputQueue, ex));
            this.criticalError = criticalError;
            if (settings.PurgeOnStartup)
            {
                //inputQueue.Purge();
            }

            return Task.FromResult(0);
        }

        public void Start(PushRuntimeSettings limitations)
        {
            runningReceiveTasks = new ConcurrentDictionary<Task, Task>();
            concurrencyLimiter = new SemaphoreSlim(limitations.MaxConcurrency);
            cancellationTokenSource = new CancellationTokenSource();

            cancellationToken = cancellationTokenSource.Token;

            subscription = connectionManager.GetConnection().ConnectToPersistentSubscription(inputQueue, inputQueue, OnEvent, SubscriptionDropped);
        }

        public async Task Stop()
        {
            subscription.Stop(TimeSpan.FromSeconds(60));
            cancellationTokenSource.Cancel();

            // ReSharper disable once MethodSupportsCancellation
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var finishedTask = await Task.WhenAny(Task.WhenAll(runningReceiveTasks.Values.ToArray()), timeoutTask).ConfigureAwait(false);

            if (finishedTask.Equals(timeoutTask))
            {
                Logger.Error("The message pump failed to stop with in the time allowed(30s)");
            }

            concurrencyLimiter.Dispose();
            runningReceiveTasks.Clear();
        }

        CancellationToken cancellationToken;
        CancellationTokenSource cancellationTokenSource;
        SemaphoreSlim concurrencyLimiter;
        CriticalError criticalError;
        RepeatedFailuresOverTimeCircuitBreaker receiveCircuitBreaker;
        ConcurrentDictionary<Task, Task> runningReceiveTasks;

        string inputQueue;
        EventStorePersistentSubscription subscription;
        Func<PushContext, Task> pipeline;

        static ILog Logger = LogManager.GetLogger<MessagePump>();
    }
}