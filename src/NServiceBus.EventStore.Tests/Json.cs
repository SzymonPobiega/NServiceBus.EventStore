using System;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NServiceBus.Transports.EventStore.Serializers.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace NServiceBus.EventStore.Tests
{
    static class Json
    {
        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };

        public static byte[] ToJsonBytes(this object source)
        {
            string instring = JsonConvert.SerializeObject(source, Formatting.Indented, JsonSettings);
            return JsonNoBomMessageSerializer.UTF8NoBom.GetBytes(instring);
        }

        public static string ToJson(this object source)
        {
            string instring = JsonConvert.SerializeObject(source, Formatting.Indented, JsonSettings);
            return instring;
        }

        public static string ToCanonicalJson(this object source)
        {
            string instring = JsonConvert.SerializeObject(source);
            return instring;
        }

        public static T ParseJson<T>(this string json)
        {
            var result = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            return result;
        }

        public static T ParseJson<T>(this byte[] json)
        {
            var result = JsonConvert.DeserializeObject<T>(JsonNoBomMessageSerializer.UTF8NoBom.GetString(json), JsonSettings);
            return result;
        }
    }
}