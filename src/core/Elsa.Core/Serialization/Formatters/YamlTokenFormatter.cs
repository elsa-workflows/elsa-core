using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using YamlDotNet.Serialization;

namespace Elsa.Serialization.Formatters
{
    public class YamlTokenFormatter : ITokenFormatter
    {
        public const string FormatName = "YAML";
        private readonly ISerializer serializer;
        private readonly IDeserializer deserializer;
        private readonly JsonSerializerSettings jsonSerializerSettings;

        public YamlTokenFormatter()
        {
            serializer = new SerializerBuilder().Build();
            deserializer = new DeserializerBuilder().Build();

            jsonSerializerSettings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
            };
                
            jsonSerializerSettings
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)
                .Converters.Add(new ExpandoObjectConverter());
        }

        public string Format => FormatName;
        public string ContentType => "application/x-yaml";

        public string ToString(JToken token)
        {
            var json = token.ToString(Formatting.None);
            var expandoObject = JsonConvert.DeserializeObject<ExpandoObject>(json, jsonSerializerSettings);
            return serializer.Serialize(expandoObject);
        }

        public JToken FromString(string data)
        {
            var expandoObject = deserializer.Deserialize<ExpandoObject>(data);
            var json = JsonConvert.SerializeObject(expandoObject, jsonSerializerSettings);

            return JsonConvert.DeserializeObject<JToken>(json, jsonSerializerSettings);
        }
    }
}