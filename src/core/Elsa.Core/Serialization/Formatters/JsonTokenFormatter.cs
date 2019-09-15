using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Serialization.Formatters
{
    public class JsonTokenFormatter : ITokenFormatter
    {
        public const string FormatName = "JSON";
        private readonly JsonSerializerSettings serializerSettings;

        public JsonTokenFormatter()
        {
            serializerSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    
                }
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public string Format => FormatName;
        public string ContentType => "application/json";
        public string ToString(JToken token) => token.ToString(Formatting.Indented);
        public JToken FromString(string data) => JsonConvert.DeserializeObject<JToken>(data, serializerSettings);
    }
}