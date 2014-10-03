//using System;
//using System.Threading;
//using EventStore.ClientAPI;
//using NServiceBus.Transports.EventStore;
//using NServiceBus.Transports.EventStore.Serializers.Json;

//namespace NServiceBus.Persistence.EventStore.TimeoutPersister
//{    
//    public class TimeoutHeartbeatGenerator : IWantToRunWhenBusStartsAndStops
//    {
//        private readonly TimeSpan period = TimeSpan.FromSeconds(EventStoreTimeoutPersister.ResolutionInSeconds);
//        private Timer timer;
//        private long sequence;
//        private readonly IManageEventStoreConnections connectionManager;
//        private const string TimeoutHeartbeatStream = "Timeout-Heartbeat";

//        public TimeoutHeartbeatGenerator(IManageEventStoreConnections connectionManager)
//        {
//            this.connectionManager = connectionManager;
//        }

//        public void Start()
//        {
//            var slice = connectionManager.GetConnection().ReadStreamEventsBackwardAsync(TimeoutHeartbeatStream, -1, 1, true).Result;
//            if (slice.Status == SliceReadStatus.Success)
//            {
//                var lastHeartbeat = slice.Events[0].Event.Data.ParseJson<TimeoutHeartbeatEvent>();
//                sequence = lastHeartbeat.Sequence;
//            }
//            timer = new Timer(EmitHeartbeat,null,TimeSpan.Zero,period);
//        }

//        private void EmitHeartbeat(object state)
//        {
//            sequence++;
//            var evnt = new TimeoutHeartbeatEvent
//            {
//                Sequence = sequence,
//                Resolution = EventStoreTimeoutPersister.ResolutionInSeconds
//            };
//            connectionManager.GetConnection().AppendToStreamAsync(TimeoutHeartbeatStream, ExpectedVersion.Any,
//                new EventData(Guid.NewGuid(), "TimeoutHeartbeat", true, evnt.ToJsonBytes(), new byte[0])).Wait();
//        }

//        public void Stop()
//        {
//            timer.Dispose();
//        }
//    }
//}