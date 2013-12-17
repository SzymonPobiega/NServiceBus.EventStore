using NServiceBus.Config;
using NServiceBus.Logging;
using NServiceBus.Serialization;
using NServiceBus.Transports.EventStore.Serializers.Json;

namespace NServiceBus.Transports.EventStore.Config
{
    public class EventStoreSerializationMethodValidator : IWantToRunWhenConfigurationIsComplete
    {
        static readonly ILog logger = LogManager.GetLogger(typeof(EventStoreSerializationMethodValidator));

        public IMessageSerializer Serializer { get; set; }

        public void Run()
        {
            if (!(Serializer is JsonNoBomMessageSerializer))
            {
                logger.Error("For EventStore transport to work message serialization need to be set to JsonNoBomMessageSerializer.");
            }
        }
    }
}