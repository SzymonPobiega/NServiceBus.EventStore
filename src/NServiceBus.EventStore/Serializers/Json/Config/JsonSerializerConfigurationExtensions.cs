using NServiceBus.Features;
using NServiceBus.Settings;

namespace NServiceBus.Transports.EventStore.Serializers.Json.Config
{
    public static class JsonSerializerConfigurationExtensions
    {
        /// <summary>
        /// Enables the json message serializer
        /// </summary>
        public static SerializationSettings JsonNoBom(this SerializationSettings settings)
        {
            Feature.Enable<JsonNoBomSerialization>();
            return settings;
        }
    }
}