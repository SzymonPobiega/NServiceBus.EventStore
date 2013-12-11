using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using NServiceBus.MessageInterfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NServiceBus.Serialization;

namespace NServiceBus.Transports.EventStore.Serializers.Json
{
    /// <summary>
    /// JSON and BSON base class for <see cref="IMessageSerializer"/>.
    /// </summary>
    public abstract class JsonMessageSerializerBase : IMessageSerializer
    {
#pragma warning disable 612,618
        private readonly IMessageMapper messageMapper;
#pragma warning restore 612,618

        readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.RoundtripKind }, new Transports.EventStore.Serializers.Json.Internal.XContainerConverter() }
        };

#pragma warning disable 612,618
        protected JsonMessageSerializerBase(IMessageMapper messageMapper)
#pragma warning restore 612,618
        {
            this.messageMapper = messageMapper;
        }
        
        /// <summary>
        /// Removes the wrapping array if serializing a single message 
        /// </summary>
        public bool SkipArrayWrappingForSingleMessages { get; set; }

        /// <summary>
        /// Serializes the given set of messages into the given stream.
        /// </summary>
        /// <param name="messages">Messages to serialize.</param>
        /// <param name="stream">Stream for <paramref name="messages"/> to be serialized into.</param>
        public void Serialize(object[] messages, Stream stream)
        {
            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            jsonSerializer.Binder = new Transports.EventStore.Serializers.Json.Internal.MessageSerializationBinder(messageMapper);

            var jsonWriter = CreateJsonWriter(stream);

            if (SkipArrayWrappingForSingleMessages && messages.Length == 1)
                jsonSerializer.Serialize(jsonWriter, messages[0]);
            else
                jsonSerializer.Serialize(jsonWriter, messages);

            jsonWriter.Flush();
        }

        /// <summary>
        /// Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream">Stream that contains messages.</param>
        /// <param name="messageTypes">The list of message types to deserialize. If null the types must be inferred from the serialized data.</param>
        /// <returns>Deserialized messages.</returns>
        public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
        {
            var settings = serializerSettings;

            var mostConcreteType = messageTypes != null ? messageTypes.FirstOrDefault() : null;
            var requiresDynamicDeserialization = mostConcreteType != null && mostConcreteType.IsInterface;

            if (requiresDynamicDeserialization)
            {
                settings = new JsonSerializerSettings{
                        TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                        TypeNameHandling = TypeNameHandling.None,
                        Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.RoundtripKind }, new Transports.EventStore.Serializers.Json.Internal.XContainerConverter() }
                };
            }

            var jsonSerializer = JsonSerializer.Create(settings);
            jsonSerializer.ContractResolver = new Transports.EventStore.Serializers.Json.Internal.MessageContractResolver(messageMapper);

            var reader = CreateJsonReader(stream);
            reader.Read();

            var firstTokenType = reader.TokenType;

            if (firstTokenType == JsonToken.StartArray)
            {
                if (requiresDynamicDeserialization)
                {
                    return (object[])jsonSerializer.Deserialize(reader, mostConcreteType.MakeArrayType());
                }
                return jsonSerializer.Deserialize<object[]>(reader);
            }
            if (messageTypes != null && messageTypes.Any())
            {
                return new[] {jsonSerializer.Deserialize(reader, messageTypes.First())};
            }

            return new[] {jsonSerializer.Deserialize<object>(reader)};
        }

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to 
        /// </summary>
        public string ContentType { get { return GetContentType(); } }

        protected abstract string GetContentType();

        protected abstract JsonWriter CreateJsonWriter(Stream stream);

        protected abstract JsonReader CreateJsonReader(Stream stream);
    }
}
