using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization.Formatters
{
    public class JsonTokenFormatter : ITokenFormatter
    {
        public const string FormatName = "JSON";
        public string Format => FormatName;
        public string ContentType => "application/json";
        public string ToString(JToken token) => token.ToString(Formatting.Indented);
        public JToken FromString(string data) => JToken.Parse(data);
    }
}