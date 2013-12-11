namespace NServiceBus
{
    using Features;
    using Settings;

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