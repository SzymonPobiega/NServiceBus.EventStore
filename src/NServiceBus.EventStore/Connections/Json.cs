using System;
using System.IO;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Formatting = Newtonsoft.Json.Formatting;

namespace NServiceBus.Internal
{
    static class Json
    {
        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
            {
                //ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Converters = new JsonConverter[] { new StringEnumConverter() }
            };

        static readonly Newtonsoft.Json.JsonSerializer Serializer = Newtonsoft.Json.JsonSerializer.Create(JsonSettings);

        public static byte[] ToJsonBytes(this object source)
        {
            var instring = JsonConvert.SerializeObject(source, Formatting.Indented, JsonSettings);
            return UTF8NoBom.GetBytes(instring);
        }

        public static EventData ToEventData(this object souece, string eventType)
        {
            return new EventData(Guid.NewGuid(), eventType, true, souece.ToJsonBytes(), new byte[0]);
        }

        public static string ToJson(this object source)
        {
            var instring = JsonConvert.SerializeObject(source, Formatting.Indented, JsonSettings);
            return instring;
        }

        public static T ParseJson<T>(this string json)
        {
            var result = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            return result;
        }

        public static T ParseJson<T>(this byte[] json)
        {
            var result = JsonConvert.DeserializeObject<T>(UTF8NoBom.GetString(json), JsonSettings);
            return result;
        }

        public static object ParseJson(this byte[] json, Type type)
        {
            var result = Serializer.Deserialize(new StringReader(UTF8NoBom.GetString(json)), type);
            return result;
        }

        public static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(false);
    }
}