using System.Text;

namespace NServiceBus.Transports.EventStore.Serializers.Json
{
    /// <summary>
    /// JSON message serializer.
    /// </summary>
    public class JsonNoBomMessageSerializer
    {
        public static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(false);
    }
}