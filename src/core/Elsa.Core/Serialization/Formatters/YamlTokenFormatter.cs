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
        public const string FormatName = SerializationFormats.Yaml;
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public YamlTokenFormatter()
        {
            _serializer = new SerializerBuilder().Build();
            _deserializer = new DeserializerBuilder().Build();

            _jsonSerializerSettings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
            };
                
            _jsonSerializerSettings
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)
                .Converters.Add(new ExpandoObjectConverter());
        }

        public string Format => FormatName;
        public string ContentType => "application/x-yaml";

        public string ToString(JObject token)
        {
            var json = token.ToString(Formatting.None);
            var expandoObject = JsonConvert.DeserializeObject<ExpandoObject>(json, _jsonSerializerSettings);
            return _serializer.Serialize(expandoObject);
        }

        public JObject FromString(string data)
        {
            var expandoObject = _deserializer.Deserialize<ExpandoObject>(data);
            var json = JsonConvert.SerializeObject(expandoObject, _jsonSerializerSettings);

            return JsonConvert.DeserializeObject<JObject>(json, _jsonSerializerSettings);
        }
    }
}