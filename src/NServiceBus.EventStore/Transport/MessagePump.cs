using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Extensibility;
using NServiceBus.Internal;
using NServiceBus.Logging;
using NServiceBus.Transports;

namespace NServiceBus
{
    class MessagePump : IPushMessages
    {
        
        public MessagePump(IConnectionConfiguration connectionConfiguration, Func<CriticalError, Task> onStart, Func<Task> onStop)
        {
            this.onStart = onStart;
            this.onStop = onStop;
            this.connection = connectionConfiguration.CreateConnection("MessagePump");
        }

        public async Task Init(Func<PushContext, Task> pipe, CriticalError criticalError, PushSettings settings)
        {
            pipeline = pipe;
            inputQueue = settings.InputQueue;
            receiveCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("EventStoreReceive", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to receive from " + settings.InputQueue, ex));
            this.criticalError = criticalError;
            if (settings.PurgeOnStartup)
            {
                //inputQueue.Purge();
            }
            await connection.ConnectAsync().ConfigureAwait(false);
            await onStart(criticalError).ConfigureAwait(false);
        }

        public void Start(PushRuntimeSettings limitations)
        {
            runningReceiveTasks = new ConcurrentDictionary<Task, Task>();
            concurrencyLimiter = new SemaphoreSlim(limitations.MaxConcurrency);
            cancellationTokenSource = new CancellationTokenSource();

            cancellationToken = cancellationTokenSource.Token;

            subscription = connection.ConnectToPersistentSubscription(inputQueue, inputQueue, OnEvent, SubscriptionDropped, autoAck: false);
        }

        void SubscriptionDropped(EventStorePersistentSubscriptionBase droppedSubscription, SubscriptionDropReason dropReason, Exception e)
        {
            if (dropReason == SubscriptionDropReason.UserInitiated)
            {
                return;
            }
            Logger.Error("Message pump subscription dropped: " + dropReason, e);
            try
            {
                subscription = connection.ConnectToPersistentSubscription(inputQueue, inputQueue, OnEvent, SubscriptionDropped);
            }
            catch (Exception ex)
            {
                criticalError.Raise("Can't reconnect to EventStore.", ex);
            }
        }

        void OnEvent(EventStorePersistentSubscriptionBase s, ResolvedEvent evnt)
        {
            concurrencyLimiter.Wait(cancellationToken);
            
            var tokenSource = new CancellationTokenSource();
            var pushContext = ToPushContext(evnt, tokenSource);
            if (pushContext == null) //system message
            {
                return;
            }
            var receiveTask = Task.Run(() =>
            {

                try
                {
                    pipeline(pushContext).GetAwaiter().GetResult();
                    if (tokenSource.IsCancellationRequested)
                    {
                        s.Fail(evnt, PersistentSubscriptionNakEventAction.Retry, "User requested");
                    }
                    else
                    {
                        s.Acknowledge(evnt);
                    }
                    receiveCircuitBreaker.Success();
                }
                catch (Exception ex)
                {
                    s.Fail(evnt, PersistentSubscriptionNakEventAction.Retry, "Unhandled exception");
                    receiveCircuitBreaker.Failure(ex).GetAwaiter().GetResult();
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

        PushContext ToPushContext(ResolvedEvent evnt, CancellationTokenSource tokenSource)
        {
            if (evnt.Event.EventType.StartsWith("$"))
            {
                return null;
            }
            var metadata = evnt.Event.Metadata.ParseJson<MessageMetadata>();
            if (metadata.TimeToBeReceived.HasValue && metadata.TimeToBeReceived.Value < DateTime.UtcNow)
            {
                return null;
            }
            var headers = metadata.Headers.ToDictionary(x => x.Key.ToPascalCase(), x => x.Value);
            var transportTransaction = new TransportTransaction();
            transportTransaction.Set(connection);
            var data = metadata.Empty //because EventStore inserts {}
                ? new byte[0] 
                : evnt.Event.Data;
            string contentType;
            if (headers.TryGetValue(Headers.ContentType, out contentType))
            {
                data = contentType != ContentTypes.Json
                    ? Convert.FromBase64String(Encoding.UTF8.GetString(data))
                    : data;
            }
            var context = new PushContext(metadata.MessageId, headers, new MemoryStream(data), transportTransaction, tokenSource, new ContextBag());
            return context;
        }

        

        public async Task Stop()
        {
            await onStop().ConfigureAwait(false);
            subscription.Stop(TimeSpan.FromSeconds(60));
            cancellationTokenSource.Cancel();

            // ReSharper disable once MethodSupportsCancellation
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var finishedTask = await Task.WhenAny(Task.WhenAll(runningReceiveTasks.Values.ToArray()), timeoutTask).ConfigureAwait(false);

            if (finishedTask.Equals(timeoutTask))
            {
                Logger.Error("The message pump failed to stop with in the time allowed(30s)");
            }
            connection.EnsureClosed();
            concurrencyLimiter.Dispose();
            runningReceiveTasks.Clear();
        }

        Func<CriticalError, Task> onStart;
        Func<Task> onStop;
        IEventStoreConnection connection;
        CancellationToken cancellationToken;
        CancellationTokenSource cancellationTokenSource;
        SemaphoreSlim concurrencyLimiter;
        CriticalError criticalError;
        RepeatedFailuresOverTimeCircuitBreaker receiveCircuitBreaker;
        ConcurrentDictionary<Task, Task> runningReceiveTasks;

        string inputQueue;
        EventStorePersistentSubscriptionBase subscription;
        Func<PushContext, Task> pipeline;

        static ILog Logger = LogManager.GetLogger<MessagePump>();
    }
}