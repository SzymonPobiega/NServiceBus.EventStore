using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Persistence.EventStore.TimeoutPersister.Events;
using NServiceBus.Timeout.Core;

namespace NServiceBus.Persistence.EventStore.TimeoutPersister
{
    class EventStoreTimeoutPersister : IPersistTimeouts
    {
        private const int SliceLengthInSeconds = 10;
        private readonly IManageEventStoreConnections connectionManager;
        private readonly object lockObject = new object();
        private readonly LinkedList<TimeoutLink> buffer = new LinkedList<TimeoutLink>();
        private List<TimeoutLink> lastReturnedChunk;
        private long nextEpoch;
        private const string TimeoutsCheckpointStreamName = "TimeoutsCheckpoint";

        public EventStoreTimeoutPersister(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;
        }
        
        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            lock (lockObject)
            {
                if (lastReturnedChunk != null)
                {
                    //Confirm
                    var lastEpochEnd = lastReturnedChunk.LastOrDefault(x => x.EpochEnd);
                    if (lastEpochEnd != null)
                    {
                        var checkpointEvent = new Checkpoint
                        {
                            Epoch = lastEpochEnd.Epoch
                        };
                        var eventData = new EventData(Guid.NewGuid(), "$checkpoint", true, checkpointEvent.ToJsonBytes(), new byte[0]);
                        connectionManager.GetConnection().AppendToStreamAsync(TimeoutsCheckpointStreamName, ExpectedVersion.Any, eventData).Wait();
                    }
                }

                var results = new List<TimeoutLink>();
                var current = buffer.First;
                var now = DateTime.UtcNow;
                while (current != null && current.Value.DueTime < now)
                {
                    results.Add(current.Value);
                    current = current.Next;
                    buffer.RemoveFirst();
                }
                nextTimeToRunQuery = current != null
                    ? current.Value.DueTime
                    : (nextEpoch != 0 ? ToDatetime(nextEpoch) : now.AddSeconds(1));

                lastReturnedChunk = results;
                return results.Select(x => Tuple.Create(x.Link, x.DueTime)).ToArray();
            }
        }

        public void Add(TimeoutData timeout)
        {
            var dueTime = timeout.Time;
            var epoch = ToEpoch(dueTime);
            var evnt = new Events.Timeout
            {
                Epoch = epoch,
                DueTime = dueTime,
                Data = timeout
            };
            var data = new EventData(Guid.NewGuid(), "$timeout", true, evnt.ToJsonBytes(), new byte[0]);
            connectionManager.GetConnection().AppendToStreamAsync(BuildTimeoutStreamName(timeout.SagaId), ExpectedVersion.Any, data).Wait();
        }

        internal void Read(CancellationToken token)
        {
            var connection = connectionManager.GetConnection();
            long nextEpoch = 0;
            var checkpoint = connection.ReadStreamEventsBackwardAsync(TimeoutsCheckpointStreamName, -1, 1, false).Result;
            if (checkpoint.Status == SliceReadStatus.Success)
            {
                var lastCheckpointEvent = checkpoint.Events[0].Event.Data.ParseJson<Checkpoint>();
                nextEpoch = lastCheckpointEvent.Epoch + 1;
            }
            else
            {
                while (!token.IsCancellationRequested)
                {
                    var start = connection.ReadStreamEventsBackwardAsync("TimeoutsStart", -1, 1, false).Result;
                    if (start.Status == SliceReadStatus.Success)
                    {
                        var startEvent = start.Events[0].Event.Data.ParseJson<Start>();
                        nextEpoch = startEvent.Epoch;
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
            if (token.IsCancellationRequested)
            {
                return;
            }
            while (!token.IsCancellationRequested)
            {
                while (nextEpoch > CurrentEpoch())
                {
                    Thread.Sleep(100);
                }
                var slice = connection.ReadStreamEventsBackwardAsync(BuildSliceStreamName(nextEpoch), -1, 1, false).Result;
                if (slice.Status != SliceReadStatus.Success)
                {
                    continue;
                }
                if (slice.Events[0].Event.EventType == "$slice-end") //closed slice
                {
                    var allEventsSlice = connection.ReadStreamEventsForwardAsync(BuildSliceStreamName(nextEpoch), 0, int.MaxValue, false).Result;
                    var events = allEventsSlice.Events
                        .Where(resolvedEvent => resolvedEvent.Event.EventType != "$slice-end")
                        .Select(resolvedEvent =>
                        {
                            var link = resolvedEvent.Event.Metadata.ParseJson<TimeoutLink>();
                            link.Link = Json.UTF8NoBom.GetString(resolvedEvent.Event.Data);
                            return link;
                        })
                        .OrderBy(x => x.DueTime);
                    if (events.Any())
                    {
                        lock (lockObject)
                        {
                            var current = buffer.Last;
                            while (current != null && current.Value.DueTime > events.First().DueTime)
                            {
                                current = current.Previous;
                            }
                            LinkedListNode<TimeoutLink> lastAdded = null;
                            foreach (var timeout in events)
                            {
                                while (current != null && current.Value.DueTime <= timeout.DueTime)
                                {
                                    current = current.Next;
                                }
                                lastAdded = current != null
                                    ? buffer.AddBefore(current, timeout)
                                    : buffer.AddLast(timeout);
                            }
                            // ReSharper disable PossibleNullReferenceException
                            lastAdded.Value.EpochEnd = true;
                            // ReSharper restore PossibleNullReferenceException
                        }
                    }
                    nextEpoch++;
                }
            }
        }

        private static string BuildSliceStreamName(long nextEpoch)
        {
            return "TimeoutsSlice-" + nextEpoch;
        }

        private string BuildTimeoutStreamName(Guid sagaId)
        {
            return "Timeouts-" + sagaId.ToString("N");
        }

        public void Heartbeat(CancellationToken token)
        {
            nextEpoch = CurrentEpoch();
            while (!token.IsCancellationRequested)
            {
                while (nextEpoch > CurrentEpoch())
                {
                    Thread.Sleep(10);
                }
                //From now on the next epoch become current one.
                var currentEpoch = nextEpoch;
                var evnt = new Heartbeat()
                {
                    Epoch = currentEpoch,
                };
                connectionManager.GetConnection().AppendToStreamAsync("Timeouts-Heartbeat", ExpectedVersion.Any, new[]
                {
                    new EventData(Guid.NewGuid(),"$heartbeat",true,evnt.ToJsonBytes(),new byte[0]), 
                });
                nextEpoch = currentEpoch + 1;
                var wakeUpTime = ToDatetime(nextEpoch);

                Thread.Sleep(wakeUpTime - DateTime.UtcNow);
            }
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            var parts = timeoutId.Split('@');
            var index = int.Parse(parts[0]);
            var stream = parts[1];
            var result = connectionManager.GetConnection().ReadEventAsync(stream, index, true).Result;
            if (result.Status != EventReadStatus.Success)
            {
                timeoutData = null;
                return false;
            }
            var evnt = result.Event.Value.Event.Data.ParseJson<Events.Timeout>();
            timeoutData = evnt.Data;
            timeoutData.Headers = timeoutData.Headers.ToDictionary(x => x.Key.ToPascalCase(), x => x.Value);
            return true;
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            connectionManager.GetConnection().DeleteStreamAsync(BuildTimeoutStreamName(sagaId), ExpectedVersion.Any, false).Wait();
        }

        private static long CurrentEpoch()
        {
            return ToEpoch(DateTime.UtcNow);
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
    }

}