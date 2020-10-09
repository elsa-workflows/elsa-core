using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Serialization.Formatters
{
    public class JsonTokenFormatter : ITokenFormatter
    {
        public const string FormatName = SerializationFormats.Json;
        private readonly JsonSerializerSettings _serializerSettings;

        public JsonTokenFormatter()
        {
            _serializerSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    
                }
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public string Format => FormatName;
        public string ContentType => "application/json";
        public string ToString(JObject token) => token.ToString(Formatting.Indented);
        public JObject FromString(string data) => JsonConvert.DeserializeObject<JObject>(data, _serializerSettings);
    }
}