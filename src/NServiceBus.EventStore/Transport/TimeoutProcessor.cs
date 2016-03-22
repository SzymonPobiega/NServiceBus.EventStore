using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using NServiceBus.Internal;
using NServiceBus.Logging;

namespace NServiceBus
{
    public class TimeoutProcessor
    {
        public TimeoutProcessor(Func<DateTime> currentTimeProvider, string uniqueId, IConnectionConfiguration connectionConfiguration)
        {
            this.currentTimeProvider = currentTimeProvider;
            this.uniqueId = uniqueId;
            this.connection = connectionConfiguration.CreateConnection("TimeoutProcessor");
        }

        public async Task Start()
        {
            await connection.ConnectAsync().ConfigureAwait(false);
            executionId = Guid.NewGuid();
            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            // ReSharper disable once MethodSupportsCancellation
            pollerTask = Task.Run(() => Poll(token));
        }

        async Task Poll(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await InnerPoll(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // ok, since the InnerPoll could observe the token
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to fetch timeouts from the timeout storage", ex);
                    //await circuitBreaker.Failure(ex).ConfigureAwait(false);
                }
            }
            connection.EnsureClosed();
        }

        async Task InnerPoll(CancellationToken cancellationToken)
        {
            var currentEpoch = await RestoreState().ConfigureAwait(false);
            while (!cancellationToken.IsCancellationRequested)
            {
                while (ToDatetime(currentEpoch) > currentTimeProvider())
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
                await SpinOnce(currentEpoch).ConfigureAwait(false);
                currentEpoch++;
            }
        }

        public async Task Defer(EventData eventData, DateTime dueTime, IEventStoreConnection dispatchConnection)
        {
            if (!storeInitialized)
            {
                var readResult = await dispatchConnection.ReadStreamEventsBackwardAsync(TimeoutProcessorStateStream, -1, 1, true).ConfigureAwait(false);
                if (readResult.Status == SliceReadStatus.Success)
                {
                    storeInitialized = true;
                }
                else
                {
                    throw new Exception("Timeout store has not been initialized. No timeout processor has even been running against this store.");
                }
            }
            var currentEpoch = ToEpoch(dueTime);
            while (true)
            {
                var currentEpochStream = "nsb-timeouts-" + currentEpoch;
                var sliceReadResult = await dispatchConnection.ReadStreamEventsBackwardAsync(currentEpochStream, -1, 1, true);
                if (sliceReadResult.Status == SliceReadStatus.StreamDeleted)
                {
                    if (Logger.IsDebugEnabled)
                    {
                        Logger.DebugFormat("Slice {0} deleted, moving on.", currentEpoch);
                    }
                    currentEpoch++;
                    continue;
                }
                if (sliceReadResult.Status == SliceReadStatus.Success && sliceReadResult.Events[0].Event.EventType == SliceEndEventType)
                {
                    if (Logger.IsDebugEnabled)
                    {
                        Logger.DebugFormat("Slice {0} closed, moving on.", currentEpoch);
                    }                    
                    currentEpoch++;
                    continue;
                }
                var expectedVersion = sliceReadResult.Status == SliceReadStatus.Success
                    ? sliceReadResult.Events[0].Event.EventNumber
                    : ExpectedVersion.NoStream;

                try
                {
                    await dispatchConnection.AppendToStreamAsync(currentEpochStream, expectedVersion, eventData).ConfigureAwait(false);
                    if (Logger.IsDebugEnabled)
                    {
                        Logger.DebugFormat("Stored timeout in slice {0}.", currentEpoch);
                    }
                    break;
                }
                catch (WrongExpectedVersionException)
                {
                    if (Logger.IsDebugEnabled)
                    {
                        Logger.DebugFormat("Expected version {0} but failed. Retrying.", expectedVersion);
                    }
                    //Ignore -> continue the loop
                }
            }
        }

        async Task<long> RestoreState()
        {
            var readResult = await connection.ReadStreamEventsBackwardAsync(TimeoutProcessorStateStream, -1, 1, true).ConfigureAwait(false);
            if (readResult.Status == SliceReadStatus.Success)
            {
                var e = readResult.Events[0].Event.Data.ParseJson<TimeoutProcessorStateEvent>();
                return e.LastProcessedEpoch + 1;
            }
            return CurrentEpoch();
        }

        async Task SpinOnce(long currentEpoch)
        {
            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("Processing slice {0}", currentEpoch);
            }
            var sliceEndEvent = new SliceEndEvent
            {
                ProcessorId = uniqueId
            };
            var currentEpochStream = "nsb-timeouts-" + currentEpoch;
            try
            {
                await connection.AppendToStreamAsync(currentEpochStream, ExpectedVersion.Any, sliceEndEvent.ToEventData(SliceEndEventType)).ConfigureAwait(false);
            }
            catch (StreamDeletedException)
            {
                return;
            }
            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("Marked slice {0} as closed", currentEpoch);
            }
            StreamEventsSlice sliceReadResult;
            try
            {
                sliceReadResult = await connection.ReadStreamEventsBackwardAsync(currentEpochStream, -1, MaxSlicePage, true);
            }
            catch (StreamDeletedException)
            {
                return;
            }
            if (sliceReadResult.Status != SliceReadStatus.Success)
            {
                return;
            }
            var firstSliceEndData = sliceReadResult.Events.FirstOrDefault(e => e.Event.EventType == SliceEndEventType);
            var firstSliceEnd = firstSliceEndData.Event.Data.ParseJson<SliceEndEvent>();
            if (firstSliceEnd.ProcessorId != uniqueId)
            {
                return;
            }
            var timeoutEvents = sliceReadResult.Events.Where(e => e.Event.EventType != SliceEndEventType).ToArray();
            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("Slice {0} has {1} timeouts to dispatch.", currentEpoch, timeoutEvents.Length);
            }
            foreach (var timeoutEvent in timeoutEvents)
            {
                var @event = timeoutEvent.Event;
                var metadata = @event.Metadata.ParseJson<MessageMetadata>();
                var destination = metadata.Destination;
                metadata.Destination = null;
                var eventData = new EventData(@event.EventId, @event.EventType, true, @event.Data, metadata.ToJsonBytes());
                await connection.AppendToStreamAsync(destination, ExpectedVersion.Any, eventData).ConfigureAwait(false);
            }
            var stateEvent = new TimeoutProcessorStateEvent
            {
                LastProcessedEpoch = currentEpoch
            };
            await connection.AppendToStreamAsync(TimeoutProcessorStateStream, ExpectedVersion.Any, stateEvent.ToEventData("$processor-state")).ConfigureAwait(false);
            await connection.DeleteStreamAsync(currentEpochStream, ExpectedVersion.Any, true).ConfigureAwait(false);
            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("Deleted slice {0}", currentEpoch);
            }
        }

        public Task Stop()
        {
            tokenSource.Cancel();
            pollerTask.Wait();
            return pollerTask;
        }

        long CurrentEpoch()
        {
            return ToEpoch(currentTimeProvider());
        }

        public static long ToEpoch(DateTime date)
        {
            return ((date.ToUniversalTime().Ticks - 621355968000000000) / 10000000) / SliceLengthInSeconds;
        }

        public static DateTime ToDatetime(long epoch)
        {
            var start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return start.AddSeconds(epoch * SliceLengthInSeconds);
        }

        Func<DateTime> currentTimeProvider;
        readonly string uniqueId;
        IEventStoreConnection connection;
        Guid executionId;
        CancellationTokenSource tokenSource;
        Task pollerTask;
        static ILog Logger = LogManager.GetLogger<TimeoutProcessor>();
        bool storeInitialized;
        const int SliceLengthInSeconds = 10;
        const string SliceEndEventType = "$slice-end";
        const string TimeoutProcessorStateStream = "nsb-timeout-processor-state";
        const int MaxSlicePage = 4096;
    }

    class SliceEndEvent
    {
        public string ProcessorId { get; set; } 
    }

    class TimeoutProcessorStateEvent
    {
        public long LastProcessedEpoch { get; set; }
    }
}