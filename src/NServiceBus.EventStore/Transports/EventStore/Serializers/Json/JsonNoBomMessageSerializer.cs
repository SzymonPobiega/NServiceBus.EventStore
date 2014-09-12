using System;
using System.IO;
using System.Text;
using NServiceBus.MessageInterfaces;
using Newtonsoft.Json;
using NServiceBus.Serializers.Json;

namespace NServiceBus.Transports.EventStore.Serializers.Json
{
    /// <summary>
    /// JSON message serializer.
    /// </summary>
    public class JsonNoBomMessageSerializer : JsonMessageSerializer
    {
        public static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(false);

        /// <summary>
        /// Constructor.
        /// </summary>
#pragma warning disable 612,618
        public JsonNoBomMessageSerializer(IMessageMapper messageMapper)
#pragma warning restore 612,618
            : base(messageMapper)
        {
        }

        protected override JsonReader CreateJsonReader(Stream stream)
        {
            return base.CreateJsonReader(stream);
        }
    }
}