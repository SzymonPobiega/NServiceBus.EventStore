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
    public class JsonNoBomMessageSerializer : JsonMessageSerializerBase
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
        protected override JsonWriter CreateJsonWriter(Stream stream)
        {
            var streamWriter = new StreamWriter(stream, UTF8NoBom);
            return new JsonTextWriter(streamWriter) { Formatting = Formatting.None };
        }

        protected override JsonReader CreateJsonReader(Stream stream)
        {
            var streamReader = new StreamReader(stream, UTF8NoBom);
            return new JsonTextReader(streamReader);
        }

        public T DeserializeObject<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        public object DeserializeObject(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        public string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        protected override string GetContentType()
        {
            return ContentTypes.Json;
        }
    }
}