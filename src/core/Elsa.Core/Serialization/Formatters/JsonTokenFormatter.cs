using Elsa.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Core.Serialization.Formatters
{
    public class JsonTokenFormatter : ITokenFormatter
    {
        public const string FormatName = "JSON";
        public string Format => FormatName;
        public string ContentType => "application/json";
        public string ToString(JToken token) => token.ToString(Formatting.Indented);
        public JToken FromString(string data) => JsonConvert.DeserializeObject<JToken>(data, new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
    }
}